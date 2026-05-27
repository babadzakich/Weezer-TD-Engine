using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network.Raft;

internal interface IRaftNetworkClient : System.IDisposable
{
    Task SendPacketAsync(IPEndPoint targetEndpoint, int targetNodeId, UdpPacket packet, CancellationToken cancellationToken);
    Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken);
}

internal sealed class DirectRaftNetworkClient : IRaftNetworkClient
{
    private readonly UdpClient _udpClient;

    public DirectRaftNetworkClient(UdpClient udpClient)
    {
        _udpClient = udpClient;
    }

    public Task SendPacketAsync(IPEndPoint targetEndpoint, int targetNodeId, UdpPacket packet, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packet, RaftJson.Options);
        return _udpClient.SendAsync(bytes, targetEndpoint, cancellationToken).AsTask();
    }

    public Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _udpClient.ReceiveAsync(cancellationToken).AsTask();
    }

    public void Dispose()
    {
        _udpClient.Close();
        _udpClient.Dispose();
    }
}

internal sealed class ProxyRaftNetworkClient : IRaftNetworkClient
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _proxyEndpoint;
    private readonly int _myNodeId;

    public ProxyRaftNetworkClient(UdpClient udpClient, IPEndPoint proxyEndpoint, int myNodeId)
    {
        _udpClient = udpClient;
        _proxyEndpoint = proxyEndpoint;
        _myNodeId = myNodeId;
    }

    public Task SendPacketAsync(IPEndPoint targetEndpoint, int targetNodeId, UdpPacket packet, CancellationToken cancellationToken)
    {
        var packetWithHeader = packet with { Header = new UdpPacketHeader(_myNodeId, targetNodeId) };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(packetWithHeader, RaftJson.Options);
        return _udpClient.SendAsync(bytes, _proxyEndpoint, cancellationToken).AsTask();
    }

    public Task<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _udpClient.ReceiveAsync(cancellationToken).AsTask();
    }

    public void Dispose()
    {
        _udpClient.Close();
        _udpClient.Dispose();
    }
}
