#nullable enable
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network;

/// <summary>
/// Optional test transport that funnels ALL in-game UDP traffic — game state, client
/// action requests and Raft consensus — through the Python chaos proxy in <c>proxy/</c>,
/// so latency / packet loss / isolation / partition / host-sabotage can be injected.
///
/// It activates only when the WTD_PROXY environment variables are present, so normal LAN
/// play is byte-for-byte unaffected (every call site is guarded by <see cref="Enabled"/>).
/// Lobby discovery (UDP 47777) is left on the LAN/loopback so two instances on one machine
/// still find each other and assign host/client; only the in-game channels are proxied.
///
/// Wire format — the proxy only reads <c>header.from</c>/<c>header.to</c> and forwards the
/// whole datagram verbatim, so we embed the original game JSON raw under "payload":
/// <code>{"header":{"from":&lt;nodeId&gt;,"to":0,"ch":"&lt;channel&gt;"},"payload":&lt;original-json&gt;}</code>
/// <c>to:0</c> = "broadcast to every other node". The receiver strips the envelope and
/// dispatches the payload to the handler registered for "ch".
///
/// Configuration (per process):
///   WTD_PROXY       = 1            enable
///   WTD_PROXY_ID    = 1            this node's integer id (must match proxy --nodes)
///   WTD_PROXY_SEND  = 127.0.0.1:5001  proxy port this node SENDS to
///   WTD_PROXY_BIND  = 6001         real port this node BINDS/receives on
/// </summary>
public sealed class ProxyLink : IDisposable
{
    public const string ChannelState   = "state";
    public const string ChannelRequest = "req";
    public const string ChannelRaft    = "raft";

    private static readonly object _initLock = new();
    private static bool _initialized;
    private static ProxyLink? _instance;

    /// <summary>The active proxy link, or null when proxy mode is off. Lazily initialised from env.</summary>
    public static ProxyLink? Instance { get { EnsureInit(); return _instance; } }

    /// <summary>True when WTD_PROXY mode is configured and the link started successfully.</summary>
    public static bool Enabled => Instance != null;

    private static void EnsureInit()
    {
        if (_initialized) return;
        lock (_initLock)
        {
            if (_initialized) return;
            _initialized = true;
            _instance = TryCreateFromEnvironment();
        }
    }

    private static ProxyLink? TryCreateFromEnvironment()
    {
        string? on = Environment.GetEnvironmentVariable("WTD_PROXY");
        if (string.IsNullOrEmpty(on) || on == "0" || on.Equals("false", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!int.TryParse(Environment.GetEnvironmentVariable("WTD_PROXY_ID"), out int nodeId))
        {
            Console.WriteLine("[ProxyLink] WTD_PROXY set but WTD_PROXY_ID missing/invalid — proxy disabled.");
            return null;
        }

        string? send = Environment.GetEnvironmentVariable("WTD_PROXY_SEND");
        if (string.IsNullOrEmpty(send) || !TryParseEndpoint(send, out var proxyEp))
        {
            Console.WriteLine("[ProxyLink] WTD_PROXY_SEND missing/invalid (expected ip:port) — proxy disabled.");
            return null;
        }

        if (!int.TryParse(Environment.GetEnvironmentVariable("WTD_PROXY_BIND"), out int bindPort))
        {
            Console.WriteLine("[ProxyLink] WTD_PROXY_BIND missing/invalid — proxy disabled.");
            return null;
        }

        try
        {
            var link = new ProxyLink(nodeId, proxyEp!, bindPort);
            Console.WriteLine($"[ProxyLink] ENABLED node={nodeId} send->{proxyEp} bind={bindPort}. " +
                              "All state/request/raft traffic routed through the proxy.");
            return link;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProxyLink] Failed to start: {ex.Message} — proxy disabled.");
            return null;
        }
    }

    private static bool TryParseEndpoint(string s, out IPEndPoint? ep)
    {
        ep = null;
        int i = s.LastIndexOf(':');
        if (i <= 0 || i >= s.Length - 1) return false;
        if (!IPAddress.TryParse(s[..i], out var ip)) return false;
        if (!int.TryParse(s[(i + 1)..], out int port)) return false;
        ep = new IPEndPoint(ip, port);
        return true;
    }

    // -----------------------------------------------------------------------

    private readonly int _nodeId;
    private readonly IPEndPoint _proxyEndpoint;
    private readonly UdpClient _udp;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, Action<byte[]>> _handlers = new();
    private readonly object _sendLock = new();
    private bool _disposed;

    private ProxyLink(int nodeId, IPEndPoint proxyEndpoint, int bindPort)
    {
        _nodeId = nodeId;
        _proxyEndpoint = proxyEndpoint;
        _udp = new UdpClient();
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.Client.Bind(new IPEndPoint(IPAddress.Any, bindPort));
        _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    /// <summary>Registers a handler that receives the raw payload bytes of packets on <paramref name="channel"/>.</summary>
    public void Subscribe(string channel, Action<byte[]> onPayload) => _handlers[channel] = onPayload;

    /// <summary>Wraps <paramref name="payloadJson"/> (valid game JSON) in the proxy envelope and broadcasts it (to:0).</summary>
    public void Send(string channel, byte[] payloadJson)
    {
        if (_disposed) return;
        var prefix = Encoding.UTF8.GetBytes(
            $"{{\"header\":{{\"from\":{_nodeId},\"to\":0,\"ch\":\"{channel}\"}},\"payload\":");

        var datagram = new byte[prefix.Length + payloadJson.Length + 1];
        Buffer.BlockCopy(prefix, 0, datagram, 0, prefix.Length);
        Buffer.BlockCopy(payloadJson, 0, datagram, prefix.Length, payloadJson.Length);
        datagram[^1] = (byte)'}';

        try { lock (_sendLock) _udp.Send(datagram, datagram.Length, _proxyEndpoint); }
        catch { }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udp.ReceiveAsync(ct);
                Dispatch(result.Buffer);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException)    { break; }
            catch { }
        }
    }

    private void Dispatch(byte[] datagram)
    {
        try
        {
            using var doc = JsonDocument.Parse(datagram);
            var root = doc.RootElement;
            if (!root.TryGetProperty("header", out var hdr)) return;
            if (!hdr.TryGetProperty("ch", out var chEl)) return;
            string ch = chEl.GetString() ?? "";
            if (!root.TryGetProperty("payload", out var payloadEl)) return;
            if (!_handlers.TryGetValue(ch, out var handler)) return;

            handler(Encoding.UTF8.GetBytes(payloadEl.GetRawText()));
        }
        catch { /* malformed envelope — ignore */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _udp.Dispose();
        _cts.Dispose();
    }
}
