using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace SimulationEngine.Network;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(BuildTowerRequest), "build_tower")]
[JsonDerivedType(typeof(SellTowerRequest), "sell_tower")]
[JsonDerivedType(typeof(UpgradeTowerRequest), "upgrade_tower")]
[JsonDerivedType(typeof(StartWaveRequest), "start_wave")]
public abstract class ClientRequest
{
    [JsonPropertyName("requesterId")]
    public string RequesterId { get; init; }
}

public sealed class BuildTowerRequest : ClientRequest
{
    [JsonPropertyName("zoneId")]
    public string ZoneId { get; init; } = string.Empty;

    [JsonPropertyName("towerDefinitionId")]
    public string TowerDefinitionId { get; init; } = string.Empty;
}

public sealed class SellTowerRequest : ClientRequest
{
    [JsonPropertyName("towerId")]
    public int TowerId { get; init; }
}

public sealed class UpgradeTowerRequest : ClientRequest
{
    [JsonPropertyName("towerId")]
    public int TowerId { get; init; }

    [JsonPropertyName("targetTowerId")]
    public string TargetTowerId { get; init; } = string.Empty;
}

public sealed class StartWaveRequest : ClientRequest
{
}

public interface IGameRequestSender
{
    Task SendRequestAsync(ClientRequest request, CancellationToken ct);
}
