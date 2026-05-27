using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network;

/// <summary>
/// Обнаружение лобби через UDP broadcast (LAN).
/// Порт 27015 — приём анонсов от хостов.
/// Каждый экземпляр использует отдельный случайный порт для прямой связи.
/// </summary>
public sealed class UdpLobbyDiscovery : ILobbyDiscovery
{
    private const int BroadcastPort = 27015;
    private static readonly IPEndPoint BroadcastEp = new(IPAddress.Broadcast, BroadcastPort);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly UdpClient _discoverySock;
    private readonly UdpClient _mainSock;
    private readonly int _mainPort;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _lock = new();

    private readonly Dictionary<string, DiscoveredEntry> _discovered = new();
    private HostState _host;
    private ClientState _client;
    private TaskCompletionSource<JoinResponseMsg> _pendingJoin;

    public string InstanceId { get; } = Guid.NewGuid().ToString("N");
    public string PlayerName { get; } = $"Player_{Environment.MachineName}_{Guid.NewGuid():N}";
    public string CurrentLobbyId { get { lock (_lock) return _host?.LobbyId ?? _client?.LobbyId; } }
    public bool IsHost { get { lock (_lock) return _host != null; } }

    public UdpLobbyDiscovery()
    {
        _discoverySock = new UdpClient();
        _discoverySock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        // SO_REUSEPORT = 15 (Linux) — нужен чтобы несколько процессов на одной машине могли принимать broadcast
        try { _discoverySock.Client.SetSocketOption(SocketOptionLevel.Socket, (SocketOptionName)15, true); } catch { }
        _discoverySock.Client.Bind(new IPEndPoint(IPAddress.Any, BroadcastPort));

        _mainSock = new UdpClient(0);
        _mainSock.EnableBroadcast = true;
        _mainPort = ((IPEndPoint)_mainSock.Client.LocalEndPoint).Port;

        _ = Task.Run(() => DiscoveryLoopAsync(_cts.Token));
        _ = Task.Run(() => MainLoopAsync(_cts.Token));
    }

    // ─── Host ──────────────────────────────────────────────────────────────

    public string HostLobby(string lobbyName, int maxPlayers, string hostName, int ping = 0, string raftEndpoint = null)
    {
        var lobbyId = Guid.NewGuid().ToString("N");
        lock (_lock)
        {
            _host = new HostState(lobbyId, lobbyName, hostName, maxPlayers);
            _host.AddPlayer(InstanceId, hostName, isHost: true, raftEndpoint, endpoint: null);
        }
        _ = Task.Run(() => AnnounceLoopAsync(_cts.Token));
        return lobbyId;
    }

    // ─── Client ────────────────────────────────────────────────────────────

    public IReadOnlyList<LocalLobbyInfo> GetAvailableLobbies()
    {
        lock (_lock)
        {
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-10);
            foreach (var k in _discovered.Keys.Where(k => _discovered[k].LastSeen < cutoff).ToList())
                _discovered.Remove(k);
            return _discovered.Values
                .Select(e => new LocalLobbyInfo(e.LobbyId, e.LobbyName, 0, e.CurrentPlayers, e.MaxPlayers))
                .OrderBy(l => l.Name)
                .ToArray();
        }
    }

    public bool JoinLobby(string lobbyId, string playerName, int ping = 0, string raftEndpoint = null)
    {
        DiscoveredEntry entry;
        lock (_lock) { _discovered.TryGetValue(lobbyId, out entry); }
        if (entry == null) return false;

        var tcs = new TaskCompletionSource<JoinResponseMsg>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock) { _pendingJoin = tcs; }

        Send(entry.HostEndpoint, new JoinRequestMsg
        {
            LobbyId = lobbyId, PlayerId = InstanceId, PlayerName = playerName, RaftEndpoint = raftEndpoint
        });

        var resp = Task.Run(async () =>
        {
            await Task.WhenAny(tcs.Task, Task.Delay(5000));
            return tcs.Task.IsCompletedSuccessfully ? tcs.Task.Result : null;
        }).GetAwaiter().GetResult();

        lock (_lock) { _pendingJoin = null; }
        if (resp?.Accepted != true) return false;

        lock (_lock)
        {
            _client = new ClientState(lobbyId, entry.HostEndpoint, resp.Players ?? new PlayerDto[0]);
        }
        return true;
    }

    public IReadOnlyList<LocalLobbyPlayerInfo> GetLobbyPlayers(string lobbyId)
    {
        lock (_lock)
        {
            if (_host?.LobbyId == lobbyId) return _host.GetPlayers();
            if (_client?.LobbyId == lobbyId) return _client.GetPlayers();
            return new LocalLobbyPlayerInfo[0];
        }
    }

    public void LeaveLobby()
    {
        lock (_lock)
        {
            if (_client != null)
            {
                Send(_client.HostEndpoint, new LeaveMsg { LobbyId = _client.LobbyId, PlayerId = InstanceId });
                _client = null;
            }
            _host = null;
        }
    }

    public void KeepAlive()
    {
        lock (_lock)
        {
            if (_client != null)
                Send(_client.HostEndpoint, new KeepAliveMsg { LobbyId = _client.LobbyId, PlayerId = InstanceId });
            else if (_host != null)
            {
                _host.UpdateLastSeen(InstanceId);
                if (_host.PruneStale()) PushState();
            }
        }
    }

    public void UpdatePlayerStatus(bool isReady) { }

    public bool SignalGameStart()
    {
        List<IPEndPoint> eps;
        string lobbyId;
        lock (_lock)
        {
            if (_host == null) return false;
            _host.IsGameStarted = true;
            eps = _host.GetClientEndpoints();
            lobbyId = _host.LobbyId;
        }
        var msg = new GameStartMsg { LobbyId = lobbyId };
        foreach (var ep in eps) Send(ep, msg);
        return true;
    }

    public bool IsLobbyGameStarting(string lobbyId)
    {
        lock (_lock)
        {
            if (_host?.LobbyId == lobbyId) return _host.IsGameStarted;
            if (_client?.LobbyId == lobbyId) return _client.IsGameStarted;
            return false;
        }
    }

    public IReadOnlyList<(string InstanceId, string RaftEndpoint)> GetRaftPeers(string lobbyId)
    {
        lock (_lock)
        {
            IReadOnlyList<LocalLobbyPlayerInfo> players = new LocalLobbyPlayerInfo[0];
            if (_host?.LobbyId == lobbyId) players = _host.GetPlayers();
            else if (_client?.LobbyId == lobbyId) players = _client.GetPlayers();
            return players
                .Where(p => p.RaftEndpoint != null && p.InstanceId != InstanceId)
                .Select(p => (p.InstanceId, p.RaftEndpoint))
                .ToArray();
        }
    }

    // ─── Loops ─────────────────────────────────────────────────────────────

    private async Task AnnounceLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            AnnounceMsg msg;
            lock (_lock)
            {
                if (_host == null) return;
                msg = new AnnounceMsg
                {
                    LobbyId = _host.LobbyId,
                    LobbyName = _host.LobbyName,
                    MaxPlayers = _host.MaxPlayers,
                    CurrentPlayers = _host.PlayerCount,
                    IsGameStarted = _host.IsGameStarted,
                    HostPort = _mainPort
                };
            }
            Send(BroadcastEp, msg);
            try { await Task.Delay(2000, ct); } catch (OperationCanceledException) { return; }
        }
    }

    private async Task DiscoveryLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var r = await _discoverySock.ReceiveAsync(ct);
                var ann = Deser<AnnounceMsg>(r.Buffer);
                if (ann?.T != "ann") continue;

                var hostEp = new IPEndPoint(r.RemoteEndPoint.Address, ann.HostPort);
                lock (_lock)
                {
                    // Не добавляем своё лобби
                    if (_host?.LobbyId != ann.LobbyId)
                        _discovered[ann.LobbyId] = new DiscoveredEntry(ann.LobbyId, ann.LobbyName, ann.MaxPlayers, ann.CurrentPlayers, hostEp, DateTimeOffset.UtcNow);
                }
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }

    private async Task MainLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var r = await _mainSock.ReceiveAsync(ct);
                HandleMain(r);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch { }
        }
    }

    private void HandleMain(UdpReceiveResult r)
    {
        var base_ = Deser<BaseMsg>(r.Buffer);
        switch (base_?.T)
        {
            case "join":  HandleJoin(Deser<JoinRequestMsg>(r.Buffer), r.RemoteEndPoint); break;
            case "joinr": lock (_lock) { _pendingJoin?.TrySetResult(Deser<JoinResponseMsg>(r.Buffer)); } break;
            case "ka":    HandleKa(Deser<KeepAliveMsg>(r.Buffer)); break;
            case "leave": HandleLeaveMsg(Deser<LeaveMsg>(r.Buffer)); break;
            case "state": HandleStateMsg(Deser<LobbyStateMsg>(r.Buffer)); break;
            case "start": HandleStartMsg(Deser<GameStartMsg>(r.Buffer)); break;
        }
    }

    private void HandleJoin(JoinRequestMsg req, IPEndPoint from)
    {
        if (req == null) return;
        lock (_lock)
        {
            if (_host?.LobbyId != req.LobbyId || _host.PlayerCount >= _host.MaxPlayers)
            {
                Send(from, new JoinResponseMsg { Accepted = false });
                return;
            }
            _host.AddPlayer(req.PlayerId, req.PlayerName, isHost: false, req.RaftEndpoint, from);
            Send(from, new JoinResponseMsg { Accepted = true, Players = _host.GetPlayerDtos() });
            PushState();
        }
    }

    private void HandleKa(KeepAliveMsg ka)
    {
        if (ka == null) return;
        lock (_lock)
        {
            if (_host?.LobbyId != ka.LobbyId) return;
            _host.UpdateLastSeen(ka.PlayerId);
            if (_host.PruneStale()) PushState();
        }
    }

    private void HandleLeaveMsg(LeaveMsg leave)
    {
        if (leave == null) return;
        lock (_lock)
        {
            if (_host?.LobbyId != leave.LobbyId) return;
            _host.RemovePlayer(leave.PlayerId);
            PushState();
        }
    }

    private void HandleStateMsg(LobbyStateMsg state)
    {
        if (state == null) return;
        lock (_lock)
        {
            if (_client?.LobbyId == state.LobbyId)
                _client.UpdatePlayers(state.Players ?? new PlayerDto[0]);
        }
    }

    private void HandleStartMsg(GameStartMsg start)
    {
        if (start == null) return;
        lock (_lock)
        {
            if (_client?.LobbyId == start.LobbyId)
                _client.IsGameStarted = true;
        }
    }

    private void PushState()
    {
        if (_host == null) return;
        var msg = new LobbyStateMsg { LobbyId = _host.LobbyId, Players = _host.GetPlayerDtos() };
        foreach (var ep in _host.GetClientEndpoints())
            Send(ep, msg);
    }

    private void Send(IPEndPoint ep, object msg)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(msg, msg.GetType(), Json);
            _mainSock.Send(bytes, ep);
        }
        catch { }
    }

    private static T Deser<T>(byte[] data) where T : class
    {
        try { return JsonSerializer.Deserialize<T>(data, Json); }
        catch { return null; }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try { _discoverySock.Close(); } catch { }
        try { _mainSock.Close(); } catch { }
        _cts.Dispose();
    }

    // ─── State ─────────────────────────────────────────────────────────────

    private sealed class HostState
    {
        private readonly Dictionary<string, HostPlayer> _players = new();

        public string LobbyId { get; }
        public string LobbyName { get; }
        public string HostName { get; }
        public int MaxPlayers { get; }
        public bool IsGameStarted { get; set; }
        public int PlayerCount => _players.Count;

        public HostState(string lobbyId, string lobbyName, string hostName, int maxPlayers)
        {
            LobbyId = lobbyId;
            LobbyName = lobbyName;
            HostName = hostName;
            MaxPlayers = maxPlayers;
        }

        public void AddPlayer(string id, string name, bool isHost, string raftEp, IPEndPoint endpoint)
        {
            _players[id] = new HostPlayer(id, name, isHost, raftEp, endpoint, DateTimeOffset.UtcNow);
        }

        public void RemovePlayer(string id) => _players.Remove(id);

        public void UpdateLastSeen(string id)
        {
            if (_players.TryGetValue(id, out var p))
                _players[id] = new HostPlayer(p.Id, p.Name, p.IsHost, p.RaftEndpoint, p.Endpoint, DateTimeOffset.UtcNow);
        }

        public bool PruneStale()
        {
            var cutoff = DateTimeOffset.UtcNow.AddSeconds(-15);
            var stale = _players.Where(kv => !kv.Value.IsHost && kv.Value.LastSeen < cutoff)
                                .Select(kv => kv.Key).ToList();
            foreach (var k in stale) _players.Remove(k);
            return stale.Count > 0;
        }

        public IReadOnlyList<LocalLobbyPlayerInfo> GetPlayers()
        {
            return _players.Values
                .OrderByDescending(p => p.IsHost)
                .ThenBy(p => p.Name)
                .Select(p => new LocalLobbyPlayerInfo(p.Id, p.Name, p.IsHost, 0, MaxPlayers, p.RaftEndpoint))
                .ToArray();
        }

        public PlayerDto[] GetPlayerDtos()
        {
            return _players.Values
                .Select(p => new PlayerDto { Id = p.Id, Name = p.Name, IsHost = p.IsHost, RaftEndpoint = p.RaftEndpoint })
                .ToArray();
        }

        public List<IPEndPoint> GetClientEndpoints()
        {
            return _players.Values
                .Where(p => !p.IsHost && p.Endpoint != null)
                .Select(p => p.Endpoint)
                .ToList();
        }
    }

    private sealed class HostPlayer
    {
        public string Id { get; }
        public string Name { get; }
        public bool IsHost { get; }
        public string RaftEndpoint { get; }
        public IPEndPoint Endpoint { get; }
        public DateTimeOffset LastSeen { get; }

        public HostPlayer(string id, string name, bool isHost, string raftEndpoint, IPEndPoint endpoint, DateTimeOffset lastSeen)
        {
            Id = id; Name = name; IsHost = isHost; RaftEndpoint = raftEndpoint; Endpoint = endpoint; LastSeen = lastSeen;
        }
    }

    private sealed class ClientState
    {
        private PlayerDto[] _players;

        public string LobbyId { get; }
        public IPEndPoint HostEndpoint { get; }
        public bool IsGameStarted { get; set; }

        public ClientState(string lobbyId, IPEndPoint hostEndpoint, PlayerDto[] players)
        {
            LobbyId = lobbyId;
            HostEndpoint = hostEndpoint;
            _players = players;
        }

        public void UpdatePlayers(PlayerDto[] players) => _players = players;

        public IReadOnlyList<LocalLobbyPlayerInfo> GetPlayers()
        {
            return _players.Select(p => new LocalLobbyPlayerInfo(p.Id, p.Name, p.IsHost, 0, 0, p.RaftEndpoint)).ToArray();
        }
    }

    private sealed record DiscoveredEntry(string LobbyId, string LobbyName, int MaxPlayers, int CurrentPlayers, IPEndPoint HostEndpoint, DateTimeOffset LastSeen);

    // ─── Messages ──────────────────────────────────────────────────────────

    private class BaseMsg
    {
        [JsonPropertyName("t")] public string T { get; set; } = string.Empty;
    }

    private sealed class AnnounceMsg : BaseMsg
    {
        public AnnounceMsg() { T = "ann"; }
        public string LobbyId { get; set; } = string.Empty;
        public string LobbyName { get; set; } = string.Empty;
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public bool IsGameStarted { get; set; }
        public int HostPort { get; set; }
    }

    private sealed class JoinRequestMsg : BaseMsg
    {
        public JoinRequestMsg() { T = "join"; }
        public string LobbyId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string RaftEndpoint { get; set; }
    }

    private sealed class JoinResponseMsg : BaseMsg
    {
        public JoinResponseMsg() { T = "joinr"; }
        public bool Accepted { get; set; }
        public PlayerDto[] Players { get; set; }
    }

    private sealed class KeepAliveMsg : BaseMsg
    {
        public KeepAliveMsg() { T = "ka"; }
        public string LobbyId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
    }

    private sealed class LeaveMsg : BaseMsg
    {
        public LeaveMsg() { T = "leave"; }
        public string LobbyId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
    }

    private sealed class LobbyStateMsg : BaseMsg
    {
        public LobbyStateMsg() { T = "state"; }
        public string LobbyId { get; set; } = string.Empty;
        public PlayerDto[] Players { get; set; }
    }

    private sealed class GameStartMsg : BaseMsg
    {
        public GameStartMsg() { T = "start"; }
        public string LobbyId { get; set; } = string.Empty;
    }

    private sealed class PlayerDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsHost { get; set; }
        public string RaftEndpoint { get; set; }
    }
}
