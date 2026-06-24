using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network;

/// <summary>
/// UDP-broadcast discovery for LAN/network lobbies.
/// Each host broadcasts their lobby every second; clients listen and collect discovered lobbies.
/// Replaces LocalLobbyDiscovery when players are on different machines.
/// </summary>
public sealed class UdpLobbyDiscovery : ILobbyDiscovery, IDisposable
{
    private const int BroadcastPort = 47777;
    private const int AnnounceIntervalMs = 1000;
    private const int StaleSeconds = 6;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, LobbyAnnouncement> _discovered = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _discoveredLastSeen = new();
    private readonly ConcurrentDictionary<string, PeerPlayer> _lobbyPlayers = new();

    private UdpClient _broadcaster;
    private UdpClient _listener;
    private LobbyAnnouncement _ownAnnouncement;
    private bool _disposed;

    public string InstanceId => _instanceId;
    public string PlayerName { get; } = $"Player_{Environment.MachineName}_{Guid.NewGuid():N}";
    public string CurrentLobbyId => _ownAnnouncement?.LobbyId;
    public bool IsHost => _ownAnnouncement?.IsHost ?? false;

    public UdpLobbyDiscovery()
    {
        _listener = new UdpClient();
        _listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _listener.Client.Bind(new IPEndPoint(IPAddress.Any, BroadcastPort));
        _listener.EnableBroadcast = true;

        _broadcaster = new UdpClient();
        _broadcaster.EnableBroadcast = true;

        _ = ReceiveLoopAsync(_cts.Token);
        _ = AnnounceLoopAsync(_cts.Token);
    }

    // -------------------------------------------------------------------------
    // Public API (same surface as LocalLobbyDiscovery)
    // -------------------------------------------------------------------------

    public IReadOnlyList<LocalLobbyInfo> GetAvailableLobbies()
    {
        PruneStale();
        return _discovered.Values
            .Where(a => a.IsHost)
            .Select(a => new LocalLobbyInfo(a.LobbyId, a.LobbyName, 0, a.CurrentPlayers, a.MaxPlayers))
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<LocalLobbyPlayerInfo> GetLobbyPlayers(string lobbyId)
    {
        PruneStale();
        return _lobbyPlayers.Values
            .Where(p => p.LobbyId == lobbyId)
            .OrderByDescending(p => p.IsHost)
            .ThenBy(p => p.PlayerName, StringComparer.OrdinalIgnoreCase)
            .Select(p => new LocalLobbyPlayerInfo(p.InstanceId, p.PlayerName, p.IsHost, 0, p.MaxPlayers, p.IsReady))
            .ToArray();
    }

    public string HostLobby(string lobbyName, int maxPlayers, string hostName, int ping = 0, string? levelPath = null)
    {
        string lobbyId = Guid.NewGuid().ToString("N");
        _ownAnnouncement = new LobbyAnnouncement
        {
            InstanceId = _instanceId,
            LobbyId = lobbyId,
            LobbyName = lobbyName,
            PlayerName = hostName,
            IsHost = true,
            MaxPlayers = maxPlayers,
            CurrentPlayers = 1,
            LevelPath = levelPath ?? string.Empty,
            IsGameStarted = false,
            LastWaveStartedIndex = -1,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _lobbyPlayers[_instanceId] = new PeerPlayer
        {
            InstanceId = _instanceId,
            LobbyId = lobbyId,
            PlayerName = hostName,
            IsHost = true,
            MaxPlayers = maxPlayers,
            IsReady = false,
            LastSeen = DateTimeOffset.UtcNow
        };

        return lobbyId;
    }

    public bool JoinLobby(string lobbyId, string playerName, int ping = 0)
    {
        PruneStale();
        var host = _discovered.Values.FirstOrDefault(a => a.LobbyId == lobbyId && a.IsHost);
        if (host is null) return false;
        if (_lobbyPlayers.Values.Count(p => p.LobbyId == lobbyId) >= host.MaxPlayers) return false;

        _ownAnnouncement = new LobbyAnnouncement
        {
            InstanceId = _instanceId,
            LobbyId = lobbyId,
            LobbyName = host.LobbyName,
            PlayerName = playerName,
            IsHost = false,
            MaxPlayers = host.MaxPlayers,
            CurrentPlayers = 1,
            LevelPath = host.LevelPath,
            IsGameStarted = false,
            LastWaveStartedIndex = -1,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        _lobbyPlayers[_instanceId] = new PeerPlayer
        {
            InstanceId = _instanceId,
            LobbyId = lobbyId,
            PlayerName = playerName,
            IsHost = false,
            MaxPlayers = host.MaxPlayers,
            IsReady = false,
            LastSeen = DateTimeOffset.UtcNow
        };

        return true;
    }

    public void LeaveLobby()
    {
        if (_ownAnnouncement is null) return;
        _lobbyPlayers.TryRemove(_instanceId, out _);
        _ownAnnouncement = null;
    }

    public void KeepAlive()
    {
        if (_ownAnnouncement is null) return;
        _ownAnnouncement = _ownAnnouncement with { Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        if (_lobbyPlayers.TryGetValue(_instanceId, out var p))
            _lobbyPlayers[_instanceId] = p with { LastSeen = DateTimeOffset.UtcNow };
    }

    public void UpdatePlayerStatus(bool isReady)
    {
        if (_ownAnnouncement is null) return;
        _ownAnnouncement = _ownAnnouncement with { IsReady = isReady };
        if (_lobbyPlayers.TryGetValue(_instanceId, out var p))
            _lobbyPlayers[_instanceId] = p with { IsReady = isReady, LastSeen = DateTimeOffset.UtcNow };
    }

    public bool SignalGameStart()
    {
        if (_ownAnnouncement is null || !_ownAnnouncement.IsHost) return false;
        _ownAnnouncement = _ownAnnouncement with { IsGameStarted = true };
        return true;
    }

    public bool SignalWaveStart(int waveIndex)
    {
        if (_ownAnnouncement is null || !_ownAnnouncement.IsHost) return false;
        _ownAnnouncement = _ownAnnouncement with { LastWaveStartedIndex = waveIndex };
        return true;
    }

    public int GetLobbyWaveStartIndex(string lobbyId)
    {
        var host = _discovered.Values.FirstOrDefault(a => a.LobbyId == lobbyId && a.IsHost);
        if (host is null && _ownAnnouncement?.LobbyId == lobbyId) host = _ownAnnouncement;
        return host?.LastWaveStartedIndex ?? -1;
    }

    public string? GetLobbyLevelPath(string lobbyId)
    {
        var host = _discovered.Values.FirstOrDefault(a => a.LobbyId == lobbyId && a.IsHost);
        if (host is null && _ownAnnouncement?.LobbyId == lobbyId) host = _ownAnnouncement;
        return string.IsNullOrEmpty(host?.LevelPath) ? null : host.LevelPath;
    }

    public bool IsLobbyGameStarting(string lobbyId)
    {
        var host = _discovered.Values.FirstOrDefault(a => a.LobbyId == lobbyId && a.IsHost);
        if (host is null && _ownAnnouncement?.LobbyId == lobbyId) host = _ownAnnouncement;
        return host?.IsGameStarted ?? false;
    }

    public string GetLobbyHostIp(string lobbyId)
    {
        var host = _discovered.Values.FirstOrDefault(a => a.LobbyId == lobbyId && a.IsHost);
        return host?.SourceIp;
    }

    public string GetInstanceIp(string instanceId)
    {
        return _discovered.TryGetValue(instanceId, out var a) ? a.SourceIp : null;
    }

    // -------------------------------------------------------------------------
    // UDP loops
    // -------------------------------------------------------------------------

    private async Task AnnounceLoopAsync(CancellationToken ct)
    {
        var broadcastEp = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);
        var loopbackEp  = new IPEndPoint(IPAddress.Loopback,   BroadcastPort);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (_ownAnnouncement is not null)
                {
                    int count = _lobbyPlayers.Values.Count(p => p.LobbyId == _ownAnnouncement.LobbyId);
                    _ownAnnouncement = _ownAnnouncement with
                    {
                        CurrentPlayers = count,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
                    byte[] payload = JsonSerializer.SerializeToUtf8Bytes(_ownAnnouncement, JsonOpts);
                    await _broadcaster.SendAsync(payload, broadcastEp, ct); // LAN
                    await _broadcaster.SendAsync(payload, loopbackEp,  ct); // same-machine
                }
                await Task.Delay(AnnounceIntervalMs, ct);
            }
            catch (OperationCanceledException) { break; }
            catch { /* ignore send errors */ }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _listener.ReceiveAsync(ct);
                var json = Encoding.UTF8.GetString(result.Buffer);
                var ann = JsonSerializer.Deserialize<LobbyAnnouncement>(json, JsonOpts);
                if (ann is null || ann.InstanceId == _instanceId) continue;

                ann = ann with { SourceIp = result.RemoteEndPoint.Address.ToString() };
                _discovered[ann.InstanceId] = ann;
                _discoveredLastSeen[ann.InstanceId] = DateTimeOffset.UtcNow;

                // Track player presence within the lobby we're in (or any lobby)
                _lobbyPlayers[ann.InstanceId] = new PeerPlayer
                {
                    InstanceId = ann.InstanceId,
                    LobbyId = ann.LobbyId,
                    PlayerName = ann.PlayerName,
                    IsHost = ann.IsHost,
                    MaxPlayers = ann.MaxPlayers,
                    IsReady = ann.IsReady,
                    LastSeen = DateTimeOffset.UtcNow
                };
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch { /* ignore malformed packets */ }
        }
    }

    private void PruneStale()
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-StaleSeconds);
        foreach (var key in _discovered.Keys.ToArray())
        {
            if (_discoveredLastSeen.TryGetValue(key, out var lastSeen) && lastSeen < cutoff)
            {
                _discovered.TryRemove(key, out _);
                _discoveredLastSeen.TryRemove(key, out _);
                _lobbyPlayers.TryRemove(key, out _);
            }
        }
        foreach (var key in _lobbyPlayers.Keys.ToArray())
        {
            if (_lobbyPlayers.TryGetValue(key, out var p) && p.LastSeen < cutoff)
                _lobbyPlayers.TryRemove(key, out _);
        }
    }

    // -------------------------------------------------------------------------
    // DTOs
    // -------------------------------------------------------------------------

    private sealed record LobbyAnnouncement
    {
        public string InstanceId { get; init; } = "";
        public string LobbyId { get; init; } = "";
        public string LobbyName { get; init; } = "";
        public string PlayerName { get; init; } = "";
        public bool IsHost { get; init; }
        public int MaxPlayers { get; init; }
        public int CurrentPlayers { get; init; }
        public string LevelPath { get; init; } = "";
        public bool IsGameStarted { get; init; }
        public int LastWaveStartedIndex { get; init; } = -1;
        public long Timestamp { get; init; }
        public bool IsReady { get; init; }
        [JsonIgnore] public string SourceIp { get; init; } = "";
    }

    private sealed record PeerPlayer
    {
        public string InstanceId { get; init; } = "";
        public string LobbyId { get; init; } = "";
        public string PlayerName { get; init; } = "";
        public bool IsHost { get; init; }
        public int MaxPlayers { get; init; }
        public bool IsReady { get; init; }
        public DateTimeOffset LastSeen { get; set; }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        _listener.Dispose();
        _broadcaster.Dispose();
    }
}
