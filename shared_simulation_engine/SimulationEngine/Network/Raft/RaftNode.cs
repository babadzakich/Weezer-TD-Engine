using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network.Raft;

public sealed class RaftNode : IDisposable
{
    private readonly RaftNodeOptions _options;
    private readonly IRaftNetworkClient _networkClient;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<UdpPacket>> _pendingResponses = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly object _lock = new();
    private readonly Random _random = new();

    private NodeRole _role = NodeRole.Follower;
    private int _currentTerm;
    private string? _votedFor;
    private string? _leaderId;
    private string _value = string.Empty;
    private DateTimeOffset _lastLeaderContact = DateTimeOffset.UtcNow;

    /// <summary>Fires when the known leader changes. Argument is the new leader ID, or null if leader is unknown.</summary>
    public event Action<string?>? LeaderChanged;

    /// <summary>Fires with diagnostic log lines (optional, for debugging).</summary>
    public event Action<string>? LogMessage;

    public bool IsLeader { get { lock (_lock) return _role == NodeRole.Leader; } }
    public string? LeaderId { get { lock (_lock) return _leaderId; } }
    public NodeRole Role { get { lock (_lock) return _role; } }
    public int CurrentTerm { get { lock (_lock) return _currentTerm; } }

    public RaftNode(RaftNodeOptions options)
    {
        _options = options;
        var udpClient = new UdpClient(options.Endpoint);

        if (options.ProxyEndpoint != null)
        {
            _networkClient = new ProxyRaftNetworkClient(udpClient, options.ProxyEndpoint, int.Parse(options.Id));
        }
        else
        {
            _networkClient = new DirectRaftNetworkClient(udpClient);
        }
    }

    public async Task RunAsync()
    {
        Log($"[{_options.Id}] listening on udp://{_options.Endpoint}");
        Log($"[{_options.Id}] peers: {string.Join(", ", _options.Peers.Select(p => $"{p.Id}=udp://{p.Endpoint}"))}");

        var serverTask = ReceiveLoopAsync(_shutdown.Token);
        var electionTask = RunElectionTimerAsync(_shutdown.Token);
        var heartbeatTask = RunHeartbeatLoopAsync(_shutdown.Token);

        try
        {
            await Task.WhenAll(serverTask, electionTask, heartbeatTask);
        }
        catch (OperationCanceledException) when (_shutdown.IsCancellationRequested) { }
        catch (ObjectDisposedException) when (_shutdown.IsCancellationRequested) { }

        Log($"[{_options.Id}] stopped");
    }

    public void Stop()
    {
        if (_shutdown.IsCancellationRequested) return;
        _shutdown.Cancel();
        foreach (var pending in _pendingResponses.Values)
            pending.TrySetCanceled();
    }

    public void Dispose()
    {
        Stop();
        _networkClient.Dispose();
        _shutdown.Dispose();
    }

    public RaftNodeStatus Snapshot()
    {
        lock (_lock)
        {
            return new RaftNodeStatus(
                _options.Id,
                _role.ToString(),
                _currentTerm,
                _votedFor,
                _leaderId,
                _value,
                _options.Peers);
        }
    }

    public async Task<SetValueResponse> SetValueAsync(string value, CancellationToken cancellationToken = default)
    {
        return await HandleSetValueAsync(new SetValueRequest(value), cancellationToken);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result;
            try
            {
                result = await _networkClient.ReceiveAsync(cancellationToken);
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested) { break; }
            catch (ObjectDisposedException) { break; }

            _ = Task.Run(() => HandleDatagramAsync(result, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleDatagramAsync(UdpReceiveResult datagram, CancellationToken cancellationToken)
    {
        UdpPacket? packet;
        try
        {
            packet = JsonSerializer.Deserialize<UdpPacket>(datagram.Buffer, RaftJson.Options);
        }
        catch (JsonException ex)
        {
            Log($"[{_options.Id}] bad packet from {datagram.RemoteEndPoint}: {ex.Message}");
            return;
        }

        if (packet is null) return;

        if (_pendingResponses.TryRemove(packet.RequestId, out var pendingResponse))
        {
            pendingResponse.TrySetResult(packet);
            return;
        }

        int sourceNodeId = packet.Header?.From ?? 0;
        await HandleRequestPacketAsync(packet, sourceNodeId, datagram.RemoteEndPoint, cancellationToken);
    }

    private async Task HandleRequestPacketAsync(UdpPacket packet, int sourceNodeId, IPEndPoint remoteEndpoint, CancellationToken cancellationToken)
    {
        switch (packet.Type)
        {
            case UdpMessageTypes.RequestVote:
            {
                var request = ReadPayload<RequestVoteRequest>(packet);
                await SendPacketAsync(remoteEndpoint, sourceNodeId, UdpPacket.Create(UdpMessageTypes.RequestVoteResponse, packet.RequestId, HandleRequestVote(request)), cancellationToken);
                break;
            }
            case UdpMessageTypes.AppendEntries:
            {
                var request = ReadPayload<AppendEntriesRequest>(packet);
                await SendPacketAsync(remoteEndpoint, sourceNodeId, UdpPacket.Create(UdpMessageTypes.AppendEntriesResponse, packet.RequestId, HandleAppendEntries(request)), cancellationToken);
                break;
            }
            case UdpMessageTypes.SetValue:
            {
                var request = ReadPayload<SetValueRequest>(packet);
                var response = await HandleSetValueAsync(request, cancellationToken);
                await SendPacketAsync(remoteEndpoint, sourceNodeId, UdpPacket.Create(UdpMessageTypes.SetValueResponse, packet.RequestId, response), cancellationToken);
                break;
            }
            case UdpMessageTypes.ApplyValue:
            {
                var request = ReadPayload<ApplyValueRequest>(packet);
                await SendPacketAsync(remoteEndpoint, sourceNodeId, UdpPacket.Create(UdpMessageTypes.ApplyValueResponse, packet.RequestId, HandleApplyValue(request)), cancellationToken);
                break;
            }
            default:
                if (!packet.Type.EndsWith("-response", StringComparison.OrdinalIgnoreCase))
                    Log($"[{_options.Id}] unknown message type '{packet.Type}' from {remoteEndpoint}");
                break;
        }
    }

    private RequestVoteResponse HandleRequestVote(RequestVoteRequest request)
    {
        lock (_lock)
        {
            if (request.Term < _currentTerm)
                return new RequestVoteResponse(_currentTerm, VoteGranted: false);

            if (request.Term > _currentTerm)
                BecomeFollower(request.Term, leaderId: null);

            var canVote = _votedFor is null || _votedFor == request.CandidateId;
            if (!canVote)
                return new RequestVoteResponse(_currentTerm, VoteGranted: false);

            _votedFor = request.CandidateId;
            _lastLeaderContact = DateTimeOffset.UtcNow;
            Log($"[{_options.Id}] voted for {request.CandidateId} in term {_currentTerm}");
            return new RequestVoteResponse(_currentTerm, VoteGranted: true);
        }
    }

    private AppendEntriesResponse HandleAppendEntries(AppendEntriesRequest request)
    {
        string? prevLeader;
        lock (_lock)
        {
            if (request.Term < _currentTerm)
                return new AppendEntriesResponse(_currentTerm, Success: false);

            if (request.Term > _currentTerm || _role != NodeRole.Follower)
                BecomeFollower(request.Term, request.LeaderId);

            prevLeader = _leaderId;
            _leaderId = request.LeaderId;
            _lastLeaderContact = DateTimeOffset.UtcNow;

            if (request.SyncValue is not null && _value != request.SyncValue)
            {
                _value = request.SyncValue;
                Log($"[{_options.Id}] value synchronized to '{_value}' from leader {request.LeaderId}");
            }
        }

        if (prevLeader != request.LeaderId)
            LeaderChanged?.Invoke(request.LeaderId);

        return new AppendEntriesResponse(_currentTerm, Success: true);
    }

    private async Task<SetValueResponse> HandleSetValueAsync(SetValueRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Value))
            return BuildSetValueResponse(success: false, "Value must not be empty.");

        RaftPeer? leaderPeer;
        lock (_lock)
        {
            if (_role == NodeRole.Leader)
            {
                leaderPeer = null;
            }
            else if (_leaderId is null)
            {
                return BuildSetValueResponse(success: false, "No leader known yet. Try after election.");
            }
            else
            {
                leaderPeer = _options.Peers.FirstOrDefault(p => p.Id == _leaderId);
                if (leaderPeer is null)
                    return BuildSetValueResponse(success: false, $"Leader '{_leaderId}' not in peers.");
            }
        }

        if (leaderPeer is not null)
        {
            var forwarded = await SendRequestAsync<SetValueRequest, SetValueResponse>(leaderPeer, UdpMessageTypes.SetValue, request, cancellationToken);
            return forwarded ?? BuildSetValueResponse(success: false, $"Could not forward to leader {leaderPeer.Id}.");
        }

        return await ApplyValueAsLeaderAsync(request.Value.Trim(), cancellationToken);
    }

    private SetValueResponse BuildSetValueResponse(bool success, string message)
    {
        lock (_lock)
            return new SetValueResponse(success, message, _leaderId, _value, _currentTerm);
    }

    private async Task<SetValueResponse> ApplyValueAsLeaderAsync(string value, CancellationToken cancellationToken)
    {
        int term;
        lock (_lock)
        {
            if (_role != NodeRole.Leader)
                return BuildSetValueResponse(success: false, "Not leader anymore.");
            _value = value;
            term = _currentTerm;
        }

        Log($"[{_options.Id}] value changed to '{value}' in term {term}");

        var applyTasks = _options.Peers.Select(p => SendApplyValueAsync(p, term, value, cancellationToken));
        var responses = await Task.WhenAll(applyTasks);
        var replicated = responses.Count(r => r?.Success == true) + 1;
        var hasMajority = replicated >= _options.Majority;
        var message = hasMajority
            ? $"Value '{value}' replicated to {replicated}/{_options.Peers.Count + 1} nodes."
            : $"Value changed locally, only {replicated}/{_options.Peers.Count + 1} nodes acknowledged.";

        return BuildSetValueResponse(hasMajority, message);
    }

    private ApplyValueResponse HandleApplyValue(ApplyValueRequest request)
    {
        lock (_lock)
        {
            if (request.Term < _currentTerm)
                return new ApplyValueResponse(_currentTerm, Success: false);

            if (request.Term > _currentTerm || _role != NodeRole.Follower)
                BecomeFollower(request.Term, request.LeaderId);

            _leaderId = request.LeaderId;
            _value = request.Value;
            _lastLeaderContact = DateTimeOffset.UtcNow;
            Log($"[{_options.Id}] value changed to '{_value}' by leader {request.LeaderId}");
            return new ApplyValueResponse(_currentTerm, Success: true);
        }
    }

    private async Task<ApplyValueResponse?> SendApplyValueAsync(RaftPeer peer, int term, string value, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<ApplyValueRequest, ApplyValueResponse>(
            peer,
            UdpMessageTypes.ApplyValue,
            new ApplyValueRequest(term, _options.Id, value),
            cancellationToken);
    }

    private async Task RunElectionTimerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(RandomElectionTimeout(), cancellationToken).WaitAsync(cancellationToken);

            bool shouldStartElection;
            lock (_lock)
            {
                var silence = DateTimeOffset.UtcNow - _lastLeaderContact;
                shouldStartElection = _role != NodeRole.Leader && silence >= _options.MinElectionTimeout;
            }

            if (shouldStartElection)
                await StartElectionAsync(cancellationToken);
        }
    }

    private async Task StartElectionAsync(CancellationToken cancellationToken)
    {
        int term;
        lock (_lock)
        {
            _role = NodeRole.Candidate;
            _currentTerm++;
            _votedFor = _options.Id;
            _leaderId = null;
            _lastLeaderContact = DateTimeOffset.UtcNow;
            term = _currentTerm;
        }

        Log($"[{_options.Id}] starting election for term {term}");

        var votes = 1;
        var voteTasks = _options.Peers.Select(p => RequestVoteAsync(p, term, cancellationToken)).ToList();

        while (voteTasks.Count > 0)
        {
            var completed = await Task.WhenAny(voteTasks);
            voteTasks.Remove(completed);

            var response = await completed;
            if (response is null) continue;

            bool becameLeader = false;
            lock (_lock)
            {
                if (response.Term > _currentTerm)
                {
                    BecomeFollower(response.Term, leaderId: null);
                    return;
                }

                if (_role == NodeRole.Candidate && response.Term == term && response.VoteGranted)
                    votes++;

                if (_role == NodeRole.Candidate && votes >= _options.Majority)
                {
                    _role = NodeRole.Leader;
                    _leaderId = _options.Id;
                    _lastLeaderContact = DateTimeOffset.UtcNow;
                    becameLeader = true;
                    Log($"[{_options.Id}] became leader for term {term}");
                }
            }

            if (becameLeader)
            {
                LeaderChanged?.Invoke(_options.Id);
                return;
            }
        }
    }

    private async Task<RequestVoteResponse?> RequestVoteAsync(RaftPeer peer, int term, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<RequestVoteRequest, RequestVoteResponse>(
            peer,
            UdpMessageTypes.RequestVote,
            new RequestVoteRequest(term, _options.Id),
            cancellationToken);
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        int counter = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_options.HeartbeatInterval, cancellationToken).WaitAsync(cancellationToken);

            int term;
            string? syncValue = null;
            lock (_lock)
            {
                if (_role != NodeRole.Leader) { counter = 0; continue; }
                term = _currentTerm;
                counter++;
                // Периодически (раз в ~1.25 сек) прикрепляем значение для синхронизации отставших нод
                if (counter % 5 == 0)
                    syncValue = _value;
            }

            await SendHeartbeatsAsync(term, syncValue, cancellationToken);
        }
    }

    private async Task SendHeartbeatsAsync(int term, string? syncValue, CancellationToken cancellationToken)
    {
        var tasks = _options.Peers.Select(p => SendHeartbeatAsync(p, term, syncValue, cancellationToken));
        var responses = await Task.WhenAll(tasks);

        foreach (var response in responses)
        {
            if (response is null) continue;
            lock (_lock)
            {
                if (response.Term > _currentTerm)
                {
                    BecomeFollower(response.Term, leaderId: null);
                    return;
                }
            }
        }
    }

    private async Task<AppendEntriesResponse?> SendHeartbeatAsync(RaftPeer peer, int term, string? syncValue, CancellationToken cancellationToken)
    {
        return await SendRequestAsync<AppendEntriesRequest, AppendEntriesResponse>(
            peer,
            UdpMessageTypes.AppendEntries,
            new AppendEntriesRequest(term, _options.Id, syncValue),
            cancellationToken);
    }

    private async Task<TResponse?> SendRequestAsync<TRequest, TResponse>(
        RaftPeer peer,
        string type,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var packet = UdpPacket.Create(type, requestId, request);
        var responseSource = new TaskCompletionSource<UdpPacket>(TaskCreationOptions.RunContinuationsAsynchronously);

        _pendingResponses[requestId] = responseSource;

        try
        {
            var targetNodeId = int.TryParse(peer.Id, out var id) ? id : 0;
            await SendPacketAsync(peer.Endpoint, targetNodeId, packet, cancellationToken);
            var timeoutTask = Task.Delay(_options.PacketTimeout, cancellationToken);
            var completed = await Task.WhenAny(responseSource.Task, timeoutTask);
            if (completed != responseSource.Task) return default;

            var responsePacket = await responseSource.Task;
            return responsePacket.Payload.Deserialize<TResponse>(RaftJson.Options);
        }
        catch
        {
            return default;
        }
        finally
        {
            _pendingResponses.TryRemove(requestId, out _);
        }
    }

    private Task SendPacketAsync(IPEndPoint endpoint, int targetNodeId, UdpPacket packet, CancellationToken cancellationToken)
    {
        return _networkClient.SendPacketAsync(endpoint, targetNodeId, packet, cancellationToken);
    }

    private void BecomeFollower(int term, string? leaderId)
    {
        if (_role != NodeRole.Follower || _currentTerm != term)
            Log($"[{_options.Id}] became follower in term {term}");

        _role = NodeRole.Follower;
        _currentTerm = term;
        _votedFor = null;
        _leaderId = leaderId;
        _lastLeaderContact = DateTimeOffset.UtcNow;
    }

    private TimeSpan RandomElectionTimeout()
    {
        lock (_lock)
        {
            var min = (int)_options.MinElectionTimeout.TotalMilliseconds;
            var max = (int)_options.MaxElectionTimeout.TotalMilliseconds;
            return TimeSpan.FromMilliseconds(_random.Next(min, max));
        }
    }

    private static T ReadPayload<T>(UdpPacket packet)
    {
        return packet.Payload.Deserialize<T>(RaftJson.Options)
            ?? throw new InvalidOperationException($"UDP packet '{packet.Type}' has empty or invalid payload.");
    }

    private void Log(string message) => LogMessage?.Invoke(message);
}
