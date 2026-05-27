using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace SimulationEngine.Network;

public sealed record LocalLobbyInfo(
    string LobbyId,
    string Name,
    int Ping,
    int CurrentPlayers,
    int MaxPlayers);

public sealed record LocalLobbyPlayerInfo(
    string InstanceId,
    string PlayerName,
    bool IsHost,
    int Ping,
    int MaxPlayers);

internal sealed record LocalLobbyEntry(
    string InstanceId,
    string LobbyId,
    string LobbyName,
    string PlayerName,
    bool IsHost,
    int Ping,
    int MaxPlayers,
    DateTimeOffset LastUpdated,
    bool IsGameStarted = false);

public sealed class LocalLobbyDiscovery : IDisposable
{
    private const string LocalLobbyFileName = "WeezerTD_LocalLobbies.json";
    private static readonly string LocalLobbyFilePath = Path.Combine(Path.GetTempPath(), LocalLobbyFileName);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly Mutex FileMutex = new(false, "WeezerTD_LocalLobbyDiscovery");
    private readonly string _instanceId = Guid.NewGuid().ToString("N");
    private LocalLobbyEntry _ownEntry;
    private DateTimeOffset _lastKeepAlive = DateTimeOffset.MinValue;
    private bool _disposed;

    public string InstanceId => _instanceId;
    public string PlayerName { get; } = $"Player_{Environment.MachineName}_{Guid.NewGuid():N}";
    public string CurrentLobbyId => _ownEntry?.LobbyId;
    public bool IsHost => _ownEntry?.IsHost ?? false;

    public IReadOnlyList<LocalLobbyInfo> GetAvailableLobbies()
    {
        EnsureNotDisposed();
        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            PruneStale(entries);
            if (_ownEntry is not null)
            {
                _ownEntry = _ownEntry with { LastUpdated = DateTimeOffset.UtcNow };
                UpdateOwnEntry(entries, _ownEntry);
                SaveEntries(entries);
            }

            return entries
                .GroupBy(entry => entry.LobbyId)
                .Select(group =>
                {
                    var hostEntry = group.FirstOrDefault(entry => entry.IsHost) ?? group.First();
                    return new LocalLobbyInfo(
                        hostEntry.LobbyId,
                        hostEntry.LobbyName,
                        hostEntry.Ping,
                        group.Count(),
                        hostEntry.MaxPlayers);
                })
                .OrderBy(lobby => lobby.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public IReadOnlyList<LocalLobbyPlayerInfo> GetLobbyPlayers(string lobbyId)
    {
        EnsureNotDisposed();
        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            PruneStale(entries);
            if (_ownEntry is not null && _ownEntry.LobbyId == lobbyId)
            {
                _ownEntry = _ownEntry with { LastUpdated = DateTimeOffset.UtcNow };
                UpdateOwnEntry(entries, _ownEntry);
                SaveEntries(entries);
            }

            return entries
                .Where(entry => entry.LobbyId == lobbyId)
                .OrderByDescending(entry => entry.IsHost)
                .ThenBy(entry => entry.PlayerName, StringComparer.OrdinalIgnoreCase)
                .Select(entry => new LocalLobbyPlayerInfo(
                    entry.InstanceId,
                    entry.PlayerName,
                    entry.IsHost,
                    entry.Ping,
                    entry.MaxPlayers))
                .ToArray();
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public string HostLobby(string lobbyName, int maxPlayers, string hostName, int ping = 0)
    {
        EnsureNotDisposed();
        _ownEntry = new LocalLobbyEntry(
            _instanceId,
            Guid.NewGuid().ToString("N"),
            lobbyName,
            hostName,
            true,
            ping,
            maxPlayers,
            DateTimeOffset.UtcNow);

        SaveOwnEntry();
        return _ownEntry.LobbyId;
    }

    public bool JoinLobby(string lobbyId, string playerName, int ping = 0)
    {
        EnsureNotDisposed();
        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            PruneStale(entries);

            var lobbyEntries = entries.Where(entry => entry.LobbyId == lobbyId).ToList();
            if (lobbyEntries.Count == 0) return false;

            var hostEntry = lobbyEntries.FirstOrDefault(entry => entry.IsHost);
            if (hostEntry is null) return false;

            if (lobbyEntries.Count >= hostEntry.MaxPlayers) return false;

            _ownEntry = new LocalLobbyEntry(
                _instanceId,
                lobbyId,
                hostEntry.LobbyName,
                playerName,
                false,
                ping,
                hostEntry.MaxPlayers,
                DateTimeOffset.UtcNow);

            UpdateOwnEntry(entries, _ownEntry);
            SaveEntries(entries);
            return true;
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public void LeaveLobby()
    {
        EnsureNotDisposed();
        AcquireMutex();
        try
        {
            if (_ownEntry is null) return;
            var entries = LoadEntries();
            entries.RemoveAll(entry => entry.InstanceId == _instanceId);
            SaveEntries(entries);
            _ownEntry = null;
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public void KeepAlive()
    {
        EnsureNotDisposed();
        if (_ownEntry is null) return;
        if (DateTimeOffset.UtcNow - _lastKeepAlive < TimeSpan.FromSeconds(2)) return;

        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            PruneStale(entries);
            _ownEntry = _ownEntry with { LastUpdated = DateTimeOffset.UtcNow };
            UpdateOwnEntry(entries, _ownEntry);
            SaveEntries(entries);
            _lastKeepAlive = DateTimeOffset.UtcNow;
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public void UpdatePlayerStatus(bool isReady)
    {
        EnsureNotDisposed();
        if (_ownEntry is null) return;

        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            PruneStale(entries);
            _ownEntry = _ownEntry with { LastUpdated = DateTimeOffset.UtcNow };
            UpdateOwnEntry(entries, _ownEntry);
            SaveEntries(entries);
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public bool SignalGameStart()
    {
        EnsureNotDisposed();
        if (_ownEntry is null || !_ownEntry.IsHost) return false;

        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            PruneStale(entries);
            _ownEntry = _ownEntry with { IsGameStarted = true, LastUpdated = DateTimeOffset.UtcNow };
            UpdateOwnEntry(entries, _ownEntry);
            SaveEntries(entries);
            return true;
        }
        finally
        {
            ReleaseMutex();
        }
    }

    public bool IsLobbyGameStarting(string lobbyId)
    {
        EnsureNotDisposed();
        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            var hostEntry = entries.FirstOrDefault(e => e.LobbyId == lobbyId && e.IsHost);
            return hostEntry is not null && hostEntry.IsGameStarted;
        }
        finally
        {
            ReleaseMutex();
        }
    }

    private void SaveOwnEntry()
    {
        AcquireMutex();
        try
        {
            var entries = LoadEntries();
            PruneStale(entries);
            if (_ownEntry is not null)
            {
                UpdateOwnEntry(entries, _ownEntry);
                SaveEntries(entries);
            }
        }
        finally
        {
            ReleaseMutex();
        }
    }

    private static void UpdateOwnEntry(List<LocalLobbyEntry> entries, LocalLobbyEntry entry)
    {
        int index = entries.FindIndex(e => e.InstanceId == entry.InstanceId);
        if (index >= 0)
        {
            entries[index] = entry;
        }
        else
        {
            entries.Add(entry);
        }
    }

    private static List<LocalLobbyEntry> LoadEntries()
    {
        if (!File.Exists(LocalLobbyFilePath))
        {
            return new List<LocalLobbyEntry>();
        }

        try
        {
            using var stream = new FileStream(LocalLobbyFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (stream.Length == 0) return new List<LocalLobbyEntry>();

            return JsonSerializer.Deserialize<List<LocalLobbyEntry>>(stream, JsonOptions) ?? new List<LocalLobbyEntry>();
        }
        catch
        {
            return new List<LocalLobbyEntry>();
        }
    }

    private static void SaveEntries(List<LocalLobbyEntry> entries)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        using (var tempStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(tempStream, entries, JsonOptions);
            tempStream.Flush(true);
        }

        File.Move(tempPath, LocalLobbyFilePath, true);
    }

    private static void PruneStale(List<LocalLobbyEntry> entries)
    {
        var cutoff = DateTimeOffset.UtcNow - TimeSpan.FromSeconds(10);
        entries.RemoveAll(entry => entry.LastUpdated < cutoff);
    }

    private static void AcquireMutex()
    {
        try
        {
            FileMutex.WaitOne(TimeSpan.FromSeconds(5));
        }
        catch (AbandonedMutexException)
        {
            // Another process terminated without releasing; we can still continue.
        }
    }

    private static void ReleaseMutex()
    {
        try
        {
            FileMutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Ignore if the mutex has not been acquired by this thread.
        }
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(LocalLobbyDiscovery));
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _ownEntry = null;
    }
}
