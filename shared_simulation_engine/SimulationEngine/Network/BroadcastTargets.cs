#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SimulationEngine.Network;

/// <summary>
/// Caches the directed-broadcast address of every up, non-loopback IPv4 interface.
///
/// Enumerating NICs via <see cref="NetworkInterface.GetAllNetworkInterfaces"/> +
/// <see cref="NetworkInterface.GetIPProperties"/> is expensive on Windows (tens of ms,
/// especially with several virtual adapters). Doing it per game tick — as the host's
/// 60 Hz FrameDelta broadcast used to — collapses the host to ~1 FPS. This helper
/// computes the list once and only refreshes it every <see cref="RefreshInterval"/>,
/// so the hot path becomes a cheap array read.
/// </summary>
internal static class BroadcastTargets
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(10);
    private static readonly object _lock = new();
    private static IPAddress[] _cached = Array.Empty<IPAddress>();
    private static DateTime _refreshedAtUtc = DateTime.MinValue;

    /// <summary>Subnet broadcast address of every active IPv4 interface (cached, refreshed lazily).</summary>
    public static IPAddress[] Get()
    {
        lock (_lock)
        {
            if (_cached.Length > 0 && DateTime.UtcNow - _refreshedAtUtc < RefreshInterval)
                return _cached;

            _cached = Compute();
            _refreshedAtUtc = DateTime.UtcNow;
            return _cached;
        }
    }

    private static IPAddress[] Compute()
    {
        var result = new List<IPAddress>();
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;

                foreach (var unicast in ni.GetIPProperties().UnicastAddresses)
                {
                    if (unicast.Address.AddressFamily != AddressFamily.InterNetwork) continue;

                    var ipBytes = unicast.Address.GetAddressBytes();
                    var maskBytes = unicast.IPv4Mask?.GetAddressBytes();
                    if (maskBytes == null || maskBytes.Length != 4) continue;

                    var broadcastBytes = new byte[4];
                    for (int i = 0; i < 4; i++)
                        broadcastBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);

                    result.Add(new IPAddress(broadcastBytes));
                }
            }
        }
        catch
        {
            // Ignore — fall through to the global-broadcast fallback below.
        }

        if (result.Count == 0)
            result.Add(IPAddress.Broadcast);

        return result.ToArray();
    }
}
