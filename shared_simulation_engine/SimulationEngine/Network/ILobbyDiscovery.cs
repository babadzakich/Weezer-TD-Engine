using System;
using System.Collections.Generic;

namespace SimulationEngine.Network;

public interface ILobbyDiscovery : IDisposable
{
    event Action<string, string> OnRemoteTowerPlace;
    event Action OnRemoteWaveStart;

    string InstanceId { get; }
    string PlayerName { get; }
    string CurrentLobbyId { get; }
    bool IsHost { get; }

    void BroadcastTowerPlace(string buildZoneId, string towerDefId);
    void BroadcastWaveStart();

    string HostLobby(string lobbyName, int maxPlayers, string hostName, int ping = 0, string raftEndpoint = null, string levelArchiveName = null);
    bool JoinLobby(string lobbyId, string playerName, int ping = 0, string raftEndpoint = null);
    string GetCurrentLobbyLevelName();
    IReadOnlyList<LocalLobbyInfo> GetAvailableLobbies();
    IReadOnlyList<LocalLobbyPlayerInfo> GetLobbyPlayers(string lobbyId);
    void LeaveLobby();
    void KeepAlive();
    void UpdatePlayerStatus(bool isReady);
    bool SignalGameStart();
    bool IsLobbyGameStarting(string lobbyId);
    IReadOnlyList<(string InstanceId, string RaftEndpoint)> GetRaftPeers(string lobbyId);
    void ProbeHost(string ipOrHost);
}
