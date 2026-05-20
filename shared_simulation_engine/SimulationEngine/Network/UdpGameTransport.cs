using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network;

public readonly struct GamePacket
{
    public string     PeerId { get; init; }
    public FrameDelta Delta  { get; init; }
}

public sealed class UdpGameTransport : IAsyncDisposable
{
    private const int Mtu            = 1400;
    private const int HeaderSize     = 6;  
    private const int MaxFragPayload = Mtu - HeaderSize;

    private readonly UdpClient _udpClient;
    private readonly ConcurrentDictionary<string, PeerState> _peersByEndpoint = new();
    private readonly ConcurrentDictionary<string, PeerState> _peersById       = new();
    private readonly CancellationTokenSource _cts = new();
    private uint _nextMsgId;

    public event Action<GamePacket> MessageReceived;

    public UdpGameTransport(int localPort)
    {
        _udpClient = new UdpClient(localPort);
    }

    public void AddPeer(string peerId, IPEndPoint remoteEndpoint)
    {
        var state = new PeerState(peerId, remoteEndpoint);
        _peersByEndpoint[remoteEndpoint.ToString()] = state;
        _peersById[peerId] = state;
    }

    public void Start() => _ = RunReceiveLoopAsync(_cts.Token);

    public Task SendFrameDeltaAsync(string peerId, FrameDelta delta, CancellationToken ct)
    {
        if (!_peersById.TryGetValue(peerId, out var peer)) return Task.CompletedTask;
        return SendAsync(peer, FrameDeltaSerializer.Serialize(delta), ct);
    }

    public Task BroadcastFrameDeltaAsync(FrameDelta delta, CancellationToken ct)
    {
        var payload = FrameDeltaSerializer.Serialize(delta);
        return Task.WhenAll(_peersById.Values.Select(p => SendAsync(p, payload, ct)));
    }

    private async Task SendAsync(PeerState peer, byte[] payload, CancellationToken ct)
    {
        uint msgId     = Interlocked.Increment(ref _nextMsgId);
        int totalFrags = (payload.Length + MaxFragPayload - 1) / MaxFragPayload;

        for (int i = 0; i < totalFrags; i++)
        {
            int offset  = i * MaxFragPayload;
            int fragLen = Math.Min(MaxFragPayload, payload.Length - offset);

            var udpPacket = new byte[HeaderSize + fragLen];
            BinaryPrimitives.WriteUInt32LittleEndian(udpPacket, msgId);
            udpPacket[4] = (byte)i;
            udpPacket[5] = (byte)totalFrags;
            payload.AsSpan(offset, fragLen).CopyTo(udpPacket.AsSpan(HeaderSize));

            await _udpClient.SendAsync(udpPacket, peer.Endpoint, ct);
        }
    }

    private async Task RunReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient.ReceiveAsync(ct);
                var raw    = result.Buffer;
                var key    = result.RemoteEndPoint.ToString();

                if (raw.Length < HeaderSize) continue;
                if (!_peersByEndpoint.TryGetValue(key, out var peer)) continue;

                uint msgId      = BinaryPrimitives.ReadUInt32LittleEndian(raw);
                byte fragIdx    = raw[4];
                byte totalFrags = raw[5];

                var assembled = peer.Reassembler.Add(msgId, fragIdx, totalFrags, raw.AsMemory(HeaderSize));
                if (assembled is null) continue;

                var delta = FrameDeltaSerializer.Deserialize(assembled);
                if (delta is null) continue;

                MessageReceived?.Invoke(new GamePacket { PeerId = peer.Id, Delta = delta });
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }
            catch (SocketException)            { break; }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _udpClient.Close();
        _udpClient.Dispose();
    }
}

internal sealed class PeerState(string id, IPEndPoint endpoint)
{
    public string             Id          { get; } = id;
    public IPEndPoint         Endpoint    { get; } = endpoint;
    public MessageReassembler Reassembler { get; } = new();
}

internal sealed class MessageReassembler
{
    private readonly Dictionary<uint, Assembly> _buffer = [];
    private uint _lastCompleted;

    public byte[] Add(uint msgId, byte fragIdx, byte totalFrags, ReadOnlyMemory<byte> data)
    {
        if (msgId <= _lastCompleted) return null;

        if (!_buffer.TryGetValue(msgId, out var assembly))
        {
            assembly = new Assembly(totalFrags);
            _buffer[msgId] = assembly;
        }

        if (!assembly.AddFragment(fragIdx, data)) return null;

        _buffer.Remove(msgId);
        _lastCompleted = msgId;

        foreach (var old in _buffer.Keys.Where(k => k < msgId).ToList())
            _buffer.Remove(old);

        return assembly.Build();
    }

    private sealed class Assembly(int totalFrags)
    {
        private readonly ReadOnlyMemory<byte>[] _frags    = new ReadOnlyMemory<byte>[totalFrags];
        private readonly bool[]                 _received = new bool[totalFrags];
        private int _count;

        public bool AddFragment(byte idx, ReadOnlyMemory<byte> data)
        {
            if (idx >= _frags.Length || _received[idx]) return false;
            _frags[idx]    = data;
            _received[idx] = true;
            return ++_count == _frags.Length;
        }

        public byte[] Build()
        {
            var result = new byte[_frags.Sum(f => f.Length)];
            int pos = 0;
            foreach (var frag in _frags) { frag.Span.CopyTo(result.AsSpan(pos)); pos += frag.Length; }
            return result;
        }
    }
}
