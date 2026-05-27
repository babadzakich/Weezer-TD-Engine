using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimulationEngine.Network.Raft;

public sealed record RaftPeer(string Id, string Host, int Port)
{
    public string Address => $"{Host}:{Port}";

    [JsonIgnore]
    public IPEndPoint Endpoint => new(IPAddress.Parse(Host), Port);
}

internal sealed record UdpPacketHeader(
    [property: JsonPropertyName("from")] int From,
    [property: JsonPropertyName("to")] int To
);

internal sealed record UdpPacket(string Type, string RequestId, JsonElement Payload, [property: JsonPropertyName("header")] UdpPacketHeader? Header = null)
{
    public static UdpPacket Create<TPayload>(string type, string requestId, TPayload payload, UdpPacketHeader? header = null)
    {
        return new UdpPacket(type, requestId, JsonSerializer.SerializeToElement(payload, RaftJson.Options), header);
    }
}

internal static class UdpMessageTypes
{
    public const string RequestVote = "request-vote";
    public const string RequestVoteResponse = "request-vote-response";
    public const string AppendEntries = "append-entries";
    public const string AppendEntriesResponse = "append-entries-response";
    public const string SetValue = "set-value";
    public const string SetValueResponse = "set-value-response";
    public const string ApplyValue = "apply-value";
    public const string ApplyValueResponse = "apply-value-response";
}

internal static class RaftJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };
}

internal sealed record RequestVoteRequest(int Term, string CandidateId);

internal sealed record RequestVoteResponse(int Term, bool VoteGranted);

internal sealed record AppendEntriesRequest(int Term, string LeaderId, string? SyncValue = null);

internal sealed record AppendEntriesResponse(int Term, bool Success);

internal sealed record SetValueRequest(string Value);

public sealed record SetValueResponse(
    bool Success,
    string Message,
    string? LeaderId,
    string? Value,
    int Term);

internal sealed record ApplyValueRequest(int Term, string LeaderId, string Value);

internal sealed record ApplyValueResponse(int Term, bool Success);

public sealed record RaftNodeStatus(
    string Id,
    string Role,
    int CurrentTerm,
    string? VotedFor,
    string? LeaderId,
    string Value,
    IReadOnlyList<RaftPeer> Peers);
