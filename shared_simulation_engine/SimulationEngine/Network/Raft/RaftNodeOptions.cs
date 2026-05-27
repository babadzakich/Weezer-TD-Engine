using System;
using System.Collections.Generic;
using System.Net;

namespace SimulationEngine.Network.Raft;

public sealed record RaftNodeOptions(
    string Id,
    IPEndPoint Endpoint,
    IReadOnlyList<RaftPeer> Peers,
    TimeSpan MinElectionTimeout,
    TimeSpan MaxElectionTimeout,
    TimeSpan HeartbeatInterval,
    TimeSpan PacketTimeout,
    IPEndPoint? ProxyEndpoint = null)
{
    public int Majority => (Peers.Count + 1) / 2 + 1;

    public static RaftNodeOptions Create(
        string id,
        IPEndPoint endpoint,
        IReadOnlyList<RaftPeer> peers,
        IPEndPoint? proxyEndpoint = null)
    {
        return new RaftNodeOptions(
            id,
            endpoint,
            peers,
            MinElectionTimeout: TimeSpan.FromMilliseconds(800),
            MaxElectionTimeout: TimeSpan.FromMilliseconds(1600),
            HeartbeatInterval: TimeSpan.FromMilliseconds(250),
            PacketTimeout: TimeSpan.FromMilliseconds(500),
            proxyEndpoint);
    }
}
