using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SimulationEngine.Network;

// ---------------------------------------------------------------------------
// Enums
// ---------------------------------------------------------------------------

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnemyStatus { Normal, Slowed, Stunned, Burning }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BulletBehavior { Linear, Homing, Piercing, Area }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BulletImpactType { Mob, Wall, Expired }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameOverReason { DefenseFailed, WaveComplete }

// ---------------------------------------------------------------------------
// FrameDelta — основной пакет каждого тика
//
// Позиции ПЕРЕДАЮТСЯ, потому что плагины делают движение недетерминированным.
// Чтобы не гонять лишнее, данные разделены на два уровня:
//
//   enemies[] / bullets[] — минимальный тик-срез (только то, что меняется каждый кадр)
//   EnemySpawnedEvent / BulletSpawnedEvent — полное описание сущности (один раз при создании)
//
// При реконнекте: мастер отправляет StateSnapshot + все FrameDelta с seq > snapshot.seq.
// ---------------------------------------------------------------------------

public sealed class FrameDelta
{
    /// <summary>Монотонный счётчик тиков.</summary>
    [JsonPropertyName("seq")]
    public long Seq { get; init; }

    [JsonPropertyName("ts")]
    public double Ts { get; init; }

    /// <summary>
    /// Глобальное состояние: деньги, жизни и т.д.
    /// Не выводится из событий — дешевле передавать напрямую.
    /// </summary>
    [JsonPropertyName("global")]
    public GlobalState Global { get; init; } = new();

    /// <summary>
    /// Враги, у которых изменилась позиция, hp ИЛИ статус с предыдущего тика.
    /// Враги, которых здесь нет — не изменились, клиент держит их последнее известное состояние.
    /// Тип, путь, скорость — уже известны клиенту из EnemySpawnedEvent.
    /// </summary>
    [JsonPropertyName("enemies")]
    public IReadOnlyList<EnemyTick> Enemies { get; init; } = [];

    /// <summary>
    /// Снаряды, изменившие позицию с предыдущего тика.
    /// Снаряды, которых здесь нет — не двигались.
    /// Снаряд исчезает из всех последующих тиков после BulletImpactEvent.
    /// Все прочие параметры — из BulletSpawnedEvent.
    /// </summary>
    [JsonPropertyName("bullets")]
    public IReadOnlyList<BulletTick> Bullets { get; init; } = [];

    /// <summary>
    /// Дискретные события тика: спавны, смерти, постройки башен, конец волны и т.д.
    /// Нужны для звуков, анимаций и для того, чтобы клиент знал параметры новых сущностей.
    /// </summary>
    [JsonPropertyName("events")]
    public IReadOnlyList<GameEvent> Events { get; init; } = [];
}

// ---------------------------------------------------------------------------
// Тик-срезы (компактные, только runtime-состояние)
// ---------------------------------------------------------------------------

/// <summary>Позиция и состояние врага в данном тике.</summary>
public sealed class EnemyTick
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("hp")]
    public int Hp { get; init; }

    [JsonPropertyName("status")]
    public EnemyStatus Status { get; init; } = EnemyStatus.Normal;
}

/// <summary>
/// Позиция снаряда в данном тике.
/// Rotation не передаётся — клиент берёт его из направления, известного с момента спавна,
/// либо вычисляет из дельты позиций если плагин меняет направление.
/// </summary>
public sealed class BulletTick
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }
}

// ---------------------------------------------------------------------------
// StateSnapshot — полный снэпшот для (ре)конекта
//
// Содержит полное описание всех сущностей (не только тик-срез).
// Клиент применяет снэпшот, затем воспроизводит FrameDelta с seq > snapshot.seq.
// ---------------------------------------------------------------------------

public sealed class StateSnapshot
{
    [JsonPropertyName("seq")]
    public long Seq { get; init; }

    [JsonPropertyName("ts")]
    public double Ts { get; init; }

    [JsonPropertyName("global")]
    public GlobalState Global { get; init; } = new();

    [JsonPropertyName("enemies")]
    public IReadOnlyList<EnemyState> Enemies { get; init; } = [];

    [JsonPropertyName("towers")]
    public IReadOnlyList<TowerState> Towers { get; init; } = [];

    [JsonPropertyName("bullets")]
    public IReadOnlyList<BulletState> Bullets { get; init; } = [];
}

// ---------------------------------------------------------------------------
// Global state
// ---------------------------------------------------------------------------

public sealed class GlobalState
{
    [JsonPropertyName("money")]
    public int Money { get; init; }

    [JsonPropertyName("playerMoney")]
    public Dictionary<string, int> PlayerMoney { get; init; } = new(System.StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("lives")]
    public int Lives { get; init; }

    [JsonPropertyName("waveIdx")]
    public int WaveIdx { get; init; }

    [JsonPropertyName("waveActive")]
    public bool WaveActive { get; init; }

    [JsonPropertyName("gameOver")]
    public bool GameOver { get; init; }

    [JsonPropertyName("won")]
    public bool Won { get; init; }
}

// ---------------------------------------------------------------------------
// Полные описания сущностей — используются только в StateSnapshot
// ---------------------------------------------------------------------------

public sealed class EnemyState
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("typeId")]
    public string TypeId { get; init; } = "";

    [JsonPropertyName("pathId")]
    public string PathId { get; init; } = "";

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("hp")]
    public int Hp { get; init; }

    [JsonPropertyName("maxHp")]
    public int MaxHp { get; init; }

    [JsonPropertyName("speed")]
    public float Speed { get; init; }

    [JsonPropertyName("wpIdx")]
    public int WpIdx { get; init; }

    [JsonPropertyName("status")]
    public EnemyStatus Status { get; init; } = EnemyStatus.Normal;
}

public sealed class TowerState
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("zoneId")]
    public string ZoneId { get; init; } = "";

    [JsonPropertyName("behaviorId")]
    public string BehaviorId { get; init; } = "";

    [JsonPropertyName("owner")]
    public string Owner { get; init; } = "";

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("cooldown")]
    public float Cooldown { get; init; }
}

public sealed class BulletState
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("behaviorId")]
    public string BehaviorId { get; init; } = "";

    [JsonPropertyName("behavior")]
    public BulletBehavior Behavior { get; init; } = BulletBehavior.Linear;

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("dx")]
    public float Dx { get; init; }

    [JsonPropertyName("dy")]
    public float Dy { get; init; }

    [JsonPropertyName("speed")]
    public float Speed { get; init; }

    [JsonPropertyName("maxDist")]
    public float MaxDist { get; init; }

    [JsonPropertyName("dmg")]
    public float Dmg { get; init; }

    [JsonPropertyName("hitRadius")]
    public float HitRadius { get; init; }

    [JsonPropertyName("targetId")]
    public int? TargetId { get; init; }

    /// <summary>Время жизни пули в секундах на момент снятия снэпшота — для продолжения симуляции.</summary>
    [JsonPropertyName("elapsed")]
    public float Elapsed { get; init; }
}

// ---------------------------------------------------------------------------
// Events
// ---------------------------------------------------------------------------

[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(EnemySpawnedEvent),       "spawn")]
[JsonDerivedType(typeof(EnemyKilledEvent),         "kill")]
[JsonDerivedType(typeof(EnemyReachedGoalEvent),    "goal")]
[JsonDerivedType(typeof(BulletSpawnedEvent),       "bullet_spawn")]
[JsonDerivedType(typeof(BulletImpactEvent),        "impact")]
[JsonDerivedType(typeof(TowerPlacedEvent),         "place")]
[JsonDerivedType(typeof(TowerPlaceRejectedEvent),  "place_rejected")]
[JsonDerivedType(typeof(TowerRemovedEvent),        "remove")]
[JsonDerivedType(typeof(TowerUpgradedEvent),       "upgrade")]
[JsonDerivedType(typeof(WaveStartedEvent),         "wave_start")]
[JsonDerivedType(typeof(WaveEndedEvent),           "wave_end")]
[JsonDerivedType(typeof(GameOverEvent),            "game_over")]
public abstract class GameEvent { }

/// <summary>
/// Полное описание нового врага — достаточно, чтобы клиент создал объект с нужным типом/путём.
/// После этого позиция приходит каждый тик в EnemyTick.
/// </summary>
public sealed class EnemySpawnedEvent : GameEvent
{
    [JsonPropertyName("eid")]
    public int EnemyId { get; init; }

    [JsonPropertyName("typeId")]
    public string TypeId { get; init; } = "";

    [JsonPropertyName("spawnPt")]
    public string SpawnPointId { get; init; } = "";

    [JsonPropertyName("pathId")]
    public string PathId { get; init; } = "";

    [JsonPropertyName("hp")]
    public int MaxHp { get; init; }

    [JsonPropertyName("speed")]
    public float Speed { get; init; }
}

public sealed class EnemyKilledEvent : GameEvent
{
    [JsonPropertyName("eid")]
    public int EnemyId { get; init; }

    [JsonPropertyName("reward")]
    public int Reward { get; init; }
}

public sealed class EnemyReachedGoalEvent : GameEvent
{
    [JsonPropertyName("eid")]
    public int EnemyId { get; init; }

    [JsonPropertyName("dmg")]
    public int Damage { get; init; }

    [JsonPropertyName("baseHpAfter")]
    public int BaseHpAfter { get; init; }

    [JsonPropertyName("mobsLeft")]
    public int MobsRemaining { get; init; }
}

/// <summary>
/// Полное описание нового снаряда — клиент создаёт объект нужного поведения.
/// После этого позиция приходит каждый тик в BulletTick.
/// Параметры (скорость, урон и т.д.) нужны клиенту даже для плагинных поведений —
/// за их интерпретацию отвечает сам плагин.
/// </summary>
public sealed class BulletSpawnedEvent : GameEvent
{
    [JsonPropertyName("bid")]
    public int BulletId { get; init; }

    [JsonPropertyName("tid")]
    public int TowerId { get; init; }

    [JsonPropertyName("behaviorId")]
    public string BehaviorId { get; init; } = "";

    [JsonPropertyName("behavior")]
    public BulletBehavior Behavior { get; init; }

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("dx")]
    public float Dx { get; init; }

    [JsonPropertyName("dy")]
    public float Dy { get; init; }

    [JsonPropertyName("speed")]
    public float Speed { get; init; }

    [JsonPropertyName("maxDist")]
    public float MaxDist { get; init; }

    [JsonPropertyName("dmg")]
    public float Dmg { get; init; }

    [JsonPropertyName("hitRadius")]
    public float HitRadius { get; init; }

    [JsonPropertyName("targetId")]
    public int? TargetId { get; init; }
}

public sealed class BulletImpactEvent : GameEvent
{
    [JsonPropertyName("bid")]
    public int BulletId { get; init; }

    [JsonPropertyName("eid")]
    public int? EnemyId { get; init; }

    [JsonPropertyName("x")]
    public float X { get; init; }

    [JsonPropertyName("y")]
    public float Y { get; init; }

    [JsonPropertyName("dmg")]
    public float Damage { get; init; }

    [JsonPropertyName("isKill")]
    public bool IsKill { get; init; }

    [JsonPropertyName("impactType")]
    public BulletImpactType ImpactType { get; init; }
}

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

/// <summary>Sent by host when a client's tower placement is rejected (e.g. zone already occupied).</summary>
public sealed class TowerPlaceRejectedEvent : GameEvent
{
    [JsonPropertyName("zoneId")]
    public string ZoneId { get; init; } = "";

    /// <summary>InstanceId of the client whose request was rejected.</summary>
    [JsonPropertyName("requesterId")]
    public string RequesterId { get; init; } = "";
}

public sealed class TowerRemovedEvent : GameEvent
{
    [JsonPropertyName("tid")]
    public int TowerId { get; init; }

    [JsonPropertyName("zoneId")]
    public string ZoneId { get; init; } = "";

    [JsonPropertyName("refund")]
    public int Refund { get; init; }

    /// <summary>InstanceId клиента, отправившего запрос (для валидации владельца на хосте).</summary>
    [JsonPropertyName("owner")]
    public string Owner { get; init; } = "";
}

public sealed class TowerUpgradedEvent : GameEvent
{
    [JsonPropertyName("tid")]
    public int TowerId { get; init; }

    [JsonPropertyName("behaviorId")]
    public string BehaviorId { get; init; } = "";

    [JsonPropertyName("prevLevel")]
    public int PrevLevel { get; init; }

    [JsonPropertyName("level")]
    public int Level { get; init; }

    [JsonPropertyName("cost")]
    public int Cost { get; init; }

    /// <summary>InstanceId клиента, отправившего запрос (для валидации владельца на хосте).</summary>
    [JsonPropertyName("owner")]
    public string Owner { get; init; } = "";
}

public sealed class WaveStartedEvent : GameEvent
{
    [JsonPropertyName("waveIdx")]
    public int WaveIdx { get; init; }

    [JsonPropertyName("totalMobs")]
    public int TotalMobs { get; init; }

    [JsonPropertyName("spawnIntervalMs")]
    public int SpawnIntervalMs { get; init; }
}

public sealed class WaveEndedEvent : GameEvent
{
    [JsonPropertyName("waveIdx")]
    public int WaveIdx { get; init; }
}

public sealed class GameOverEvent : GameEvent
{
    [JsonPropertyName("winner")]
    public string Winner { get; init; }

    [JsonPropertyName("reason")]
    public GameOverReason Reason { get; init; }

    [JsonPropertyName("scores")]
    public IReadOnlyDictionary<string, int> Scores { get; init; } = new Dictionary<string, int>();
}
