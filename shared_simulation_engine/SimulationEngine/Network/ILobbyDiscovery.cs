using System.Collections.Generic;

namespace SimulationEngine.Network;

public interface ILobbyDiscovery
{
    string InstanceId { get; }
    string PlayerName { get; }
    string CurrentLobbyId { get; }
    bool IsHost { get; }

    IReadOnlyList<LocalLobbyInfo> GetAvailableLobbies();
    IReadOnlyList<LocalLobbyPlayerInfo> GetLobbyPlayers(string lobbyId);

    string HostLobby(string lobbyName, int maxPlayers, string hostName, int ping = 0, string? levelPath = null);
    bool JoinLobby(string lobbyId, string playerName, int ping = 0);
    void LeaveLobby();
    void KeepAlive();
    void UpdatePlayerStatus(bool isReady);

    bool SignalGameStart();
    bool SignalWaveStart(int waveIndex);
    int GetLobbyWaveStartIndex(string lobbyId);
    string? GetLobbyLevelPath(string lobbyId);
    bool IsLobbyGameStarting(string lobbyId);

    /// <summary>Returns the IP address of the lobby host for direct game transport.</summary>
    string GetLobbyHostIp(string lobbyId);

    /// <summary>Returns the IP address of a specific peer by their instance ID.</summary>
    string GetInstanceIp(string instanceId);
}
