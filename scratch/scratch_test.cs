using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimulationEngine.Network;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnemyStatus { Normal, Slowed, Stunned, Burning }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BulletBehavior { Linear, Homing, Piercing, Area }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BulletImpactType { Mob, Wall, Expired }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameOverReason { DefenseFailed, WaveComplete }

[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(TowerPlacedEvent),         "place")]
public abstract class GameEvent { }

public sealed class TowerPlacedEvent : GameEvent
{
    [JsonPropertyName("tid")]
    public int TowerId { get; init; }

    [JsonPropertyName("zoneId")]
    public string ZoneId { get; init; } = "";

    [JsonPropertyName("behaviorId")]
    public string BehaviorId { get; init; } = "";

    [JsonPropertyName("owner")]
    public string Owner { get; init; } = "";

    [JsonPropertyName("cost")]
    public int Cost { get; init; }
}

public sealed class FrameDelta
{
    [JsonPropertyName("events")]
    public IReadOnlyList<GameEvent> Events { get; init; } = [];
}

public class Program
{
    public static void Main()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var delta = new FrameDelta
        {
            Events = new List<GameEvent>
            {
                new TowerPlacedEvent
                {
                    TowerId = 42,
                    ZoneId = "zone_1",
                    BehaviorId = "basic",
                    Owner = "my_client_id",
                    Cost = 100
                }
            }
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(delta, options);
        var jsonStr = System.Text.Encoding.UTF8.GetString(bytes);
        Console.WriteLine("Serialized JSON: " + jsonStr);

        var deserialized = JsonSerializer.Deserialize<FrameDelta>(bytes, options);
        var evt = deserialized.Events[0] as TowerPlacedEvent;
        Console.WriteLine($"Deserialized: Owner='{evt.Owner}', TowerId={evt.TowerId}, ZoneId='{evt.ZoneId}'");
    }
}
