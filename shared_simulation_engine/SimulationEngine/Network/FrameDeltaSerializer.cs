using System;
using System.Text.Json;

namespace SimulationEngine.Network;

public static class FrameDeltaSerializer
{
    private static readonly JsonSerializerOptions CompactOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions PrettyOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    // --- FrameDelta ---

    public static byte[] Serialize(FrameDelta delta)
        => JsonSerializer.SerializeToUtf8Bytes(delta, CompactOptions);

    public static string SerializePretty(FrameDelta delta)
        => JsonSerializer.Serialize(delta, PrettyOptions);

    public static FrameDelta Deserialize(ReadOnlySpan<byte> data)
    {
        try { return JsonSerializer.Deserialize<FrameDelta>(data, CompactOptions); }
        catch (JsonException) { return null; }
    }

    public static bool TryDeserialize(ReadOnlySpan<byte> data, out FrameDelta delta)
    {
        delta = Deserialize(data);
        return delta is not null;
    }

    // --- StateSnapshot ---

    public static byte[] Serialize(StateSnapshot snapshot)
        => JsonSerializer.SerializeToUtf8Bytes(snapshot, CompactOptions);

    public static string SerializePretty(StateSnapshot snapshot)
        => JsonSerializer.Serialize(snapshot, PrettyOptions);

    public static StateSnapshot DeserializeSnapshot(ReadOnlySpan<byte> data)
    {
        try { return JsonSerializer.Deserialize<StateSnapshot>(data, CompactOptions); }
        catch (JsonException) { return null; }
    }

    public static bool TryDeserializeSnapshot(ReadOnlySpan<byte> data, out StateSnapshot snapshot)
    {
        snapshot = DeserializeSnapshot(data);
        return snapshot is not null;
    }
}
