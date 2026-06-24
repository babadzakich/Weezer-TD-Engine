using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimulationEngine.Network;

internal static class ConsensusMessageTypes
{
    public const string Ping               = "ping";
    public const string Pong               = "pong";
    public const string Announce           = "announce";   // "I'm here" broadcast sent by every node on start
    public const string RequestVote        = "request-vote";
    public const string VoteResponse       = "vote-response";
    public const string LeaderAnnouncement = "leader";
}

/// <summary>
/// UDP envelope for all Raft consensus messages (port 47780).
/// Mirrors the UdpPacket design from Raft/RaftMember/RaftMessages.cs.
/// </summary>
internal sealed class ConsensusPacket
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = "";

    /// <summary>Shared between request and matching response so we can route it to the pending TCS.</summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; init; } = "";

    /// <summary>InstanceId of the sender.</summary>
    [JsonPropertyName("from")]
    public string From { get; init; } = "";

    [JsonPropertyName("payload")]
    public JsonElement Payload { get; init; }

    /// <summary>
    /// When non-empty: only the node whose instanceId matches this field handles the request.
    /// Empty means "for all". Responses always use requestId matching via _pending, regardless of TargetId.
    /// </summary>
    [JsonPropertyName("targetId")]
    public string TargetId { get; init; } = "";

    public static ConsensusPacket Create<T>(string type, string requestId, string from, T payload, string targetId = "")
        => new()
        {
            Type      = type,
            RequestId = requestId,
            From      = from,
            TargetId  = targetId,
            Payload   = JsonSerializer.SerializeToElement(payload, ConsensusJson.Options),
        };
}

internal static class ConsensusJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false,
    };
}

// ---------------------------------------------------------------------------
// Payloads
// ---------------------------------------------------------------------------

internal sealed record PingPayload();

internal sealed record PongPayload(string ReplyTo);

internal sealed record RequestVotePayload(int Term, string CandidateId);

internal sealed record VoteResponsePayload(int Term, bool VoteGranted, string ReplyTo);

internal sealed record LeaderAnnouncementPayload(int Term, string LeaderId, string LeaderIp);
