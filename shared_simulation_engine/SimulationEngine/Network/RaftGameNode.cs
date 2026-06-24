#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network;

internal enum GameNodeRole { Follower, Candidate, Leader }

/// <summary>
/// Embedded Raft consensus node for in-game master re-election.
///
/// All game players (host + clients) run this node on UDP port 47780.
/// The current game master (host) is the "Leader" in Raft terms.
///
/// ALL consensus packets are sent as UDP broadcast (255.255.255.255 + 127.0.0.1).
/// This ensures delivery on both LAN and same-machine testing where multiple processes
/// share one port via SO_REUSEADDR (unicast to 127.0.0.1 would reach only one process).
///
/// Request routing uses ConsensusPacket.TargetId: nodes ignore request packets whose
/// TargetId doesn't match their own instanceId (empty TargetId = "anyone can respond").
/// Response routing uses _pending[requestId] TCS matching, regardless of TargetId.
///
/// Normal operation:
///   - Client receives FrameDelta from master → calls NotifyMasterAlive() to reset election timer.
///   - If no FrameDelta arrives for HeartbeatTimeout (3 s):
///       1. Ping all client peers (broadcast, any response confirms connectivity).
///       2. If any peer responds  → master is dead → start Raft election.
///       3. If no peers known     → we're alone → self-promote immediately.
///       4. If peers known but none respond → own network broken → fire OnNetworkLost.
///   - Election winner fires OnBecameLeader.
///   - All other nodes learn the new leader via broadcast → fire OnNewLeaderKnown(instanceId, ip).
///
/// Ported and adapted from Raft/RaftMember/RaftNode.cs (standalone Raft demo).
/// </summary>
public sealed class RaftGameNode : IDisposable
{
    public const int ConsensusPort = 47780;

    private static readonly TimeSpan HeartbeatTimeout   = TimeSpan.FromSeconds(3);
    // How often the leader re-broadcasts its leadership (acts as a Raft heartbeat).
    // Must be comfortably smaller than HeartbeatTimeout so followers tolerate a few losses.
    private static readonly TimeSpan HeartbeatInterval  = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ElectionTimeoutMin = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ElectionTimeoutMax = TimeSpan.FromMilliseconds(1500);
    private static readonly TimeSpan PacketTimeout      = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PingTimeout        = TimeSpan.FromSeconds(2);

    private static readonly JsonSerializerOptions JsonOpts = ConsensusJson.Options;

    private static readonly IPEndPoint LoopbackEp  = new(IPAddress.Loopback,  ConsensusPort);

    // -----------------------------------------------------------------------
    // Raft state (guarded by _lock)
    // -----------------------------------------------------------------------

    private readonly string _myId;
    private readonly object _lock = new();
    private readonly Random _rng  = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<ConsensusPacket>> _pending = new();

    private GameNodeRole _role          = GameNodeRole.Follower;
    private int          _currentTerm;
    private string?      _votedFor;
    private string?      _leaderId;
    private DateTimeOffset _lastLeaderContact = DateTimeOffset.UtcNow;
    // InstanceId of the current game master (updated on LeaderAnnouncement / becoming leader).
    // Excluded from "client peer" pings so a dead master isn't counted as an unreachable peer.
    private string? _masterInstanceId;

    // InstanceId → IP; seeded by SetPeers(), then updated from every received packet's RemoteEndPoint.
    private readonly ConcurrentDictionary<string, string> _peerIdToIp = new();

    private UdpClient? _udp;
    private bool _disposed;

    // -----------------------------------------------------------------------
    // Public events
    // -----------------------------------------------------------------------

    /// <summary>Fired on the background thread when THIS node wins an election.</summary>
    public event Action? OnBecameLeader;

    /// <summary>Fired when a leader announcement is received from another node. Args: (instanceId, ip).</summary>
    public event Action<string, string>? OnNewLeaderKnown;

    /// <summary>Fired when connectivity to all known client peers is lost (own network broken).</summary>
    public event Action? OnNetworkLost;

    // -----------------------------------------------------------------------
    // Setup
    // -----------------------------------------------------------------------

    public RaftGameNode(string myInstanceId)
    {
        _myId = myInstanceId;
    }

    /// <summary>Provides the initial peer map (instanceId → ip) before Start().</summary>
    public void SetPeers(IReadOnlyDictionary<string, string> instanceToIp)
    {
        foreach (var kv in instanceToIp)
            _peerIdToIp[kv.Key] = kv.Value;
    }

    public void Start()
    {
        _udp = new UdpClient();
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.Client.Bind(new IPEndPoint(IPAddress.Any, ConsensusPort));
        _udp.EnableBroadcast = true;

        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token), _cts.Token);
        _ = Task.Run(() => RunElectionTimerAsync(_cts.Token), _cts.Token);
        _ = Task.Run(() => AnnounceLoopAsync(_cts.Token), _cts.Token);
    }

    /// <summary>
    /// Declares this node as the initial Leader (call from the game host on game start).
    /// Immediately broadcasts a LeaderAnnouncement so all clients reset their election timers.
    /// </summary>
    public async Task SetAsLeaderAsync()
    {
        lock (_lock)
        {
            _role             = GameNodeRole.Leader;
            _currentTerm      = 1;
            _votedFor         = _myId;   // we've "voted" for ourselves this term: don't grant our vote away
            _leaderId         = _myId;
            _masterInstanceId = _myId;
            _lastLeaderContact = DateTimeOffset.UtcNow;
        }

        Console.WriteLine($"[Raft] Node {_myId} is the initial leader (term 1).");
        await AnnounceLeadershipAsync(_cts.Token);
    }

    /// <summary>
    /// Call every time a FrameDelta (game-state broadcast from master) is received.
    /// Resets the Raft election timer, like AppendEntries heartbeats in vanilla Raft.
    /// </summary>
    public void NotifyMasterAlive()
    {
        lock (_lock)
            _lastLeaderContact = DateTimeOffset.UtcNow;
    }

    public void Stop()
    {
        if (_cts.IsCancellationRequested) return;
        _cts.Cancel();
        foreach (var tcs in _pending.Values)
            tcs.TrySetCanceled();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _udp?.Dispose();
        _cts.Dispose();
    }

    // -----------------------------------------------------------------------
    // Receive loop
    // -----------------------------------------------------------------------

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp!.ReceiveAsync(ct);
                _ = Task.Run(() => HandlePacketAsync(result, ct), ct);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }
            catch { }
        }
    }

    private async Task HandlePacketAsync(UdpReceiveResult result, CancellationToken ct)
    {
        ConsensusPacket? packet;
        try { packet = JsonSerializer.Deserialize<ConsensusPacket>(result.Buffer, JsonOpts); }
        catch { return; }

        if (packet is null || packet.From == _myId) return;

        // Always learn the sender's actual IP so peer discovery is dynamic.
        _peerIdToIp[packet.From] = result.RemoteEndPoint.Address.ToString();

        // Responses are matched by requestId regardless of TargetId.
        if (_pending.TryRemove(packet.RequestId, out var tcs))
        {
            tcs.TrySetResult(packet);
            return;
        }

        // For requests: if a specific target is named and it's not us, ignore.
        if (!string.IsNullOrEmpty(packet.TargetId) && packet.TargetId != _myId)
            return;

        switch (packet.Type)
        {
            case ConsensusMessageTypes.Ping:
                await HandlePingAsync(packet, ct);
                break;
            case ConsensusMessageTypes.Announce:
                // Sender IP is already recorded in _peerIdToIp above — nothing else to do.
                break;
            case ConsensusMessageTypes.RequestVote:
                await HandleRequestVoteAsync(packet, ct);
                break;
            case ConsensusMessageTypes.LeaderAnnouncement:
                HandleLeaderAnnouncement(packet);
                break;
        }
    }

    // -----------------------------------------------------------------------
    // Ping (peer connectivity check)
    // -----------------------------------------------------------------------

    private async Task HandlePingAsync(ConsensusPacket ping, CancellationToken ct)
    {
        // Reply to the sender via broadcast with TargetId = sender's instanceId,
        // so on a shared-port machine only the original pinger's _pending[requestId] matches.
        var pong = ConsensusPacket.Create(
            ConsensusMessageTypes.Pong,
            ping.RequestId,
            _myId,
            new PongPayload(ping.From),
            targetId: ping.From);
        await BroadcastAsync(pong, ct);
    }

    /// <summary>
    /// Sends a broadcast ping (TargetId = "") and waits for any peer to respond.
    /// Returns true if at least one response arrives within PingTimeout.
    /// Works correctly on same-machine testing since all peers receive the broadcast.
    /// </summary>
    private async Task<bool> PingAnyPeerAsync(CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var ping = ConsensusPacket.Create(ConsensusMessageTypes.Ping, requestId, _myId, new PingPayload());

        var tcs = new TaskCompletionSource<ConsensusPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        try
        {
            await BroadcastAsync(ping, ct);
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(PingTimeout, ct));
            return completed == tcs.Task;
        }
        catch { return false; }
        finally { _pending.TryRemove(requestId, out _); }
    }

    /// <summary>Pings any available peer. Kept as public API for external callers.</summary>
    public Task<bool> PingRandomPeersAsync(int count = 2) => PingAnyPeerAsync(_cts.Token);

    // -----------------------------------------------------------------------
    // Announce loop — makes all nodes discoverable on the consensus port
    // -----------------------------------------------------------------------

    /// <summary>
    /// Runs every <see cref="HeartbeatInterval"/> (1 s):
    ///   - If we are the Leader, re-broadcast a LeaderAnnouncement. This is the Raft
    ///     heartbeat that keeps every follower's election timer from firing, independent
    ///     of whether game-state FrameDeltas are flowing. Without it a 3 s gap in
    ///     FrameDeltas triggers a spurious election and the master role flip-flops.
    ///   - Otherwise broadcast plain presence so all nodes learn each other's IPs.
    /// On same-machine testing, broadcast reaches all processes sharing port 47780.
    /// On LAN, broadcast reaches all nodes on the subnet (BroadcastAsync also unicasts
    /// to every known peer, so heartbeats survive broadcast-filtering firewalls).
    /// </summary>
    private async Task AnnounceLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            bool isLeader;
            lock (_lock) { isLeader = _role == GameNodeRole.Leader; }

            if (isLeader)
            {
                await AnnounceLeadershipAsync(ct);
            }
            else
            {
                var packet = ConsensusPacket.Create(
                    ConsensusMessageTypes.Announce,
                    Guid.NewGuid().ToString("N"),
                    _myId,
                    new PingPayload());
                await BroadcastAsync(packet, ct);
            }

            try { await Task.Delay(HeartbeatInterval, ct); }
            catch (OperationCanceledException) { break; }
        }
    }

    // -----------------------------------------------------------------------
    // Election timer — mirrors RunElectionTimerAsync in Raft/RaftMember/RaftNode.cs
    // -----------------------------------------------------------------------

    private async Task RunElectionTimerAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await Task.Delay(RandomElectionTimeout(), ct); }
            catch (OperationCanceledException) { break; }

            bool shouldCheck;
            lock (_lock)
            {
                var silence = DateTimeOffset.UtcNow - _lastLeaderContact;
                shouldCheck = _role != GameNodeRole.Leader && silence >= HeartbeatTimeout;
            }

            if (!shouldCheck) continue;

            Console.WriteLine($"[Raft] No master heartbeat for >{HeartbeatTimeout.TotalSeconds}s.");

            // Identify client peers: everyone except self and the (suspected dead) master.
            string? masterId;
            lock (_lock) { masterId = _masterInstanceId; }

            var otherPeers = _peerIdToIp
                .Where(kv => kv.Key != _myId)
                .ToList();
            var clientPeers = otherPeers
                .Where(kv => kv.Key != masterId)
                .ToList();

            if (otherPeers.Count == 0)
            {
                // Truly alone — no other node exists. Self-promote without voting.
                Console.WriteLine("[Raft] No peers at all — self-promoting to master.");
                await StartElectionAsync(ct);
                return;
            }

            // Probe the network. Any node (a client peer OR the master itself) that
            // answers the ping proves it is still reachable.
            bool anyAlive = await PingAnyPeerAsync(ct);

            if (clientPeers.Count == 0)
            {
                // The only other node is the master. Confirm it is really gone before
                // taking over — a brief heartbeat gap in a 2-player game must NOT cause
                // a split-brain where both nodes believe they are the host.
                if (anyAlive)
                {
                    Console.WriteLine("[Raft] Master answered ping — still alive, standing down.");
                    lock (_lock) _lastLeaderContact = DateTimeOffset.UtcNow;
                    continue;
                }
                Console.WriteLine("[Raft] Master unreachable, no other peers — self-promoting to master.");
                await StartElectionAsync(ct);
                return;
            }

            if (!anyAlive)
            {
                Console.WriteLine("[Raft] No peer responded — own network is broken.");
                OnNetworkLost?.Invoke();
                return;
            }

            Console.WriteLine("[Raft] Client peers reachable, master is dead — starting election.");
            await StartElectionAsync(ct);
        }
    }

    private async Task StartElectionAsync(CancellationToken ct)
    {
        int term;
        string? masterId;
        lock (_lock)
        {
            _role      = GameNodeRole.Candidate;
            _currentTerm++;
            _votedFor  = _myId;
            _leaderId  = null;
            _lastLeaderContact = DateTimeOffset.UtcNow;
            term     = _currentTerm;
            masterId = _masterInstanceId;
        }

        // Active client peers by instanceId (NOT by IP, to avoid Distinct() collapsing
        // multiple local processes sharing 127.0.0.1 into one slot).
        var activePeers = _peerIdToIp
            .Where(kv => kv.Key != _myId && kv.Key != masterId)
            .Select(kv => kv.Key)
            .ToList();

        int votes    = 1; // self-vote
        int majority = (activePeers.Count + 1) / 2 + 1;

        Console.WriteLine($"[Raft] Election term {term}: {activePeers.Count} peer(s), majority = {majority}.");

        // Uncontested win (no peers or sole candidate).
        if (votes >= majority)
        {
            lock (_lock) { _role = GameNodeRole.Leader; _leaderId = _myId; _masterInstanceId = _myId; }
            Console.WriteLine($"[Raft] Won election for term {term} (uncontested)!");
            await AnnounceLeadershipAsync(ct);
            OnBecameLeader?.Invoke();
            return;
        }

        // Send one broadcast RequestVote per peer, targeted by instanceId.
        // On same-machine: broadcast reaches all processes; TargetId ensures only the right one responds.
        // On LAN: broadcast reaches all machines; TargetId limits who responds.
        var voteTasks = activePeers
            .Select(peerId => RequestVoteFromPeerAsync(peerId, term, ct))
            .ToList();

        while (voteTasks.Count > 0)
        {
            var done = await Task.WhenAny(voteTasks);
            voteTasks.Remove(done);

            var response = await done;
            if (response is null) continue;

            lock (_lock)
            {
                if (response.Term > _currentTerm)
                {
                    BecomeFollower(response.Term, leaderId: null);
                    return;
                }

                if (_role == GameNodeRole.Candidate && response.Term == term && response.VoteGranted)
                    votes++;

                if (_role == GameNodeRole.Candidate && votes >= majority)
                {
                    _role             = GameNodeRole.Leader;
                    _leaderId         = _myId;
                    _masterInstanceId = _myId;
                    _lastLeaderContact = DateTimeOffset.UtcNow;
                }
            }

            bool isLeader;
            lock (_lock) { isLeader = _role == GameNodeRole.Leader; }

            if (isLeader)
            {
                Console.WriteLine($"[Raft] Won election for term {term} — becoming game master!");
                await AnnounceLeadershipAsync(ct);
                OnBecameLeader?.Invoke();
                return;
            }
        }

        Console.WriteLine($"[Raft] Election for term {term} failed (not enough votes).");
    }

    // -----------------------------------------------------------------------
    // RequestVote — mirrors RaftNode.HandleRequestVote / RequestVoteAsync
    // -----------------------------------------------------------------------

    private async Task HandleRequestVoteAsync(ConsensusPacket packet, CancellationToken ct)
    {
        var req = packet.Payload.Deserialize<RequestVotePayload>(JsonOpts)!;
        VoteResponsePayload resp;

        lock (_lock)
        {
            if (req.Term < _currentTerm)
            {
                resp = new VoteResponsePayload(_currentTerm, VoteGranted: false, req.CandidateId);
            }
            else
            {
                if (req.Term > _currentTerm)
                    BecomeFollower(req.Term, leaderId: null);

                bool canVote = _votedFor is null || _votedFor == req.CandidateId;
                if (canVote)
                {
                    _votedFor = req.CandidateId;
                    _lastLeaderContact = DateTimeOffset.UtcNow;
                    Console.WriteLine($"[Raft] Voted for {req.CandidateId} in term {_currentTerm}.");
                }
                resp = new VoteResponsePayload(_currentTerm, canVote, req.CandidateId);
            }
        }

        // Reply to the candidate via broadcast with TargetId so only the candidate processes it.
        var reply = ConsensusPacket.Create(
            ConsensusMessageTypes.VoteResponse,
            packet.RequestId,
            _myId,
            resp,
            targetId: packet.From);
        await BroadcastAsync(reply, ct);
    }

    /// <summary>Sends a targeted RequestVote broadcast to <paramref name="peerId"/> and awaits the response.</summary>
    private async Task<VoteResponsePayload?> RequestVoteFromPeerAsync(string peerId, int term, CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var req       = new RequestVotePayload(term, _myId);
        var packet    = ConsensusPacket.Create(
            ConsensusMessageTypes.RequestVote,
            requestId,
            _myId,
            req,
            targetId: peerId); // only peerId handles this request
        var tcs = new TaskCompletionSource<ConsensusPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[requestId] = tcs;

        try
        {
            await BroadcastAsync(packet, ct);

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(PacketTimeout, ct));
            if (completed != tcs.Task) return null;

            var responsePacket = await tcs.Task;
            return responsePacket.Payload.Deserialize<VoteResponsePayload>(JsonOpts);
        }
        catch { return null; }
        finally { _pending.TryRemove(requestId, out _); }
    }

    // -----------------------------------------------------------------------
    // Leader announcement broadcast
    // -----------------------------------------------------------------------

    private async Task AnnounceLeadershipAsync(CancellationToken ct)
    {
        int term;
        lock (_lock) { term = _currentTerm; }

        var payload = new LeaderAnnouncementPayload(term, _myId, GetLocalIp());
        var packet  = ConsensusPacket.Create(
            ConsensusMessageTypes.LeaderAnnouncement,
            Guid.NewGuid().ToString("N"),
            _myId,
            payload);
        await BroadcastAsync(packet, ct);
    }

    private void HandleLeaderAnnouncement(ConsensusPacket packet)
    {
        var payload = packet.Payload.Deserialize<LeaderAnnouncementPayload>(JsonOpts);
        if (payload is null) return;

        bool leaderChanged;
        lock (_lock)
        {
            if (payload.Term < _currentTerm) return;
            // Distinguish a genuine leadership change from a periodic heartbeat re-announce
            // of the same leader (which arrives every HeartbeatInterval).
            leaderChanged = _masterInstanceId != payload.LeaderId || _currentTerm != payload.Term;
            _masterInstanceId = payload.LeaderId;
            BecomeFollower(payload.Term, payload.LeaderId);
        }

        // Heartbeat from the leader we already follow — timer was reset above, stay quiet.
        if (!leaderChanged) return;

        string ip = !string.IsNullOrEmpty(payload.LeaderIp)
            ? payload.LeaderIp
            : _peerIdToIp.GetValueOrDefault(payload.LeaderId, "");

        Console.WriteLine($"[Raft] New leader: {payload.LeaderId} at {ip} (term {payload.Term}).");
        OnNewLeaderKnown?.Invoke(payload.LeaderId, ip);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private void BecomeFollower(int term, string? leaderId)
    {
        _role        = GameNodeRole.Follower;
        _currentTerm = term;
        _votedFor    = null;
        _leaderId    = leaderId;
        _lastLeaderContact = DateTimeOffset.UtcNow;
    }

    private TimeSpan RandomElectionTimeout()
    {
        lock (_lock)
        {
            int min = (int)ElectionTimeoutMin.TotalMilliseconds;
            int max = (int)ElectionTimeoutMax.TotalMilliseconds;
            return TimeSpan.FromMilliseconds(_rng.Next(min, max));
        }
    }

    private async Task BroadcastAsync(ConsensusPacket packet, CancellationToken ct)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(packet, JsonOpts);

        // 1. Broadcast on all active network interfaces (multi-NIC/virtual adapter envs).
        //    Targets are cached (see BroadcastTargets) — enumerating NICs per send is expensive on Windows.
        foreach (var subnetBroadcast in BroadcastTargets.Get())
        {
            try { await _udp!.SendAsync(data, new IPEndPoint(subnetBroadcast, ConsensusPort), ct); } catch { }
        }

        // 2. Local loopback broadcast (same-machine)
        try { await _udp!.SendAsync(data, LoopbackEp, ct); } catch { }

        // 3. Unicast directly to all known peer players (fallback direct channel)
        foreach (var ipStr in _peerIdToIp.Values)
        {
            if (!string.IsNullOrEmpty(ipStr) && IPAddress.TryParse(ipStr, out var ip))
            {
                try { await _udp!.SendAsync(data, new IPEndPoint(ip, ConsensusPort), ct); } catch { }
            }
        }
    }

    /// <summary>Best-effort detection of the machine's outbound IP address.</summary>
    private static string GetLocalIp()
    {
        try
        {
            using var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.Connect("8.8.8.8", 80);
            return ((IPEndPoint)sock.LocalEndPoint!).Address.ToString();
        }
        catch { return "127.0.0.1"; }
    }
}
