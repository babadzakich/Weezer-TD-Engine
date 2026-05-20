using System;
using System.Collections.Generic;
using System.Linq;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;
using SimulationEngine.EnemyRelated;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.MapRelated;
using SimulationEngine.WaveRelated;
using SimulationEngine.Network;
using Microsoft.Xna.Framework;

namespace SimulationEngine.Network;

public static class FrameDeltaStateSync
{
    public static FrameDelta BuildFrameDelta(GameManager gameManager, IReadOnlyCollection<GameEvent> pendingEvents, long seq)
    {
        var ui = gameManager.UIManager;
        var enemyTicks = gameManager.EnemyController?.Enemies
            .Select(e => new EnemyTick
            {
                Id = e.Id,
                X = e.Position.X,
                Y = e.Position.Y,
                Hp = e.Health,
                Status = e.Status
            })
            .ToList() ?? new List<EnemyTick>();

        var bulletTicks = gameManager.DamageDealerController?.DamageDealers
            .Select(b => new BulletTick
            {
                Id = b.Id,
                X = b.Position.X,
                Y = b.Position.Y
            })
            .ToList() ?? new List<BulletTick>();

        return new FrameDelta
        {
            Seq = seq,
            Ts = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds,
            Global = new GlobalState
            {
                Money = ui.Money,
                Lives = ui.Lives,
                WaveIdx = gameManager.WaveController?.CurrentWaveIndex ?? 0,
                WaveActive = gameManager.WaveController?.IsWaveActive ?? false
            },
            Enemies = enemyTicks,
            Bullets = bulletTicks,
            Events = pendingEvents?.ToList() ?? new List<GameEvent>()
        };
    }

    public static StateSnapshot BuildSnapshot(GameManager gameManager, long seq)
    {
        var ui = gameManager.UIManager;

        var enemyStates = gameManager.EnemyController?.Enemies
            .Select(e => new EnemyState
            {
                Id = e.Id,
                TypeId = e.TypeId,
                PathId = e.PathId,
                X = e.Position.X,
                Y = e.Position.Y,
                Hp = e.Health,
                MaxHp = e.MaxHealth,
                Speed = e.Speed,
                WpIdx = 0,
                Status = e.Status
            })
            .ToList() ?? new List<EnemyState>();

        var towerStates = gameManager.TowerController?.towers
            .Select(t => new TowerState
            {
                Id = t.Id,
                ZoneId = t.ZoneId,
                BehaviorId = t.Definition?.Id ?? t.Behavior?.GetType().Name ?? string.Empty,
                Owner = string.Empty,
                X = t.Position.X,
                Y = t.Position.Y,
                Level = t.UpgradeLevel,
                Cooldown = t.cooldownTimer
            })
            .ToList() ?? new List<TowerState>();

        var bulletStates = gameManager.DamageDealerController?.DamageDealers
            .Select(b => new BulletState
            {
                Id = b.Id,
                BehaviorId = b.BehaviorId,
                Behavior = MapBehaviorType(b.BehaviorId),
                X = b.Position.X,
                Y = b.Position.Y,
                Dx = b.Direction.X,
                Dy = b.Direction.Y,
                Speed = b.Behavior?.Speed ?? 0f,
                MaxDist = GetBulletMaxDist(b),
                Dmg = b.Behavior?.Damage ?? 0f,
                HitRadius = b.HitRadius,
                TargetId = b.TargetId,
                Elapsed = b.Elapsed
            })
            .ToList() ?? new List<BulletState>();

        return new StateSnapshot
        {
            Seq = seq,
            Ts = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds,
            Global = new GlobalState
            {
                Money = ui.Money,
                Lives = ui.Lives,
                WaveIdx = gameManager.WaveController?.CurrentWaveIndex ?? 0,
                WaveActive = gameManager.WaveController?.IsWaveActive ?? false
            },
            Enemies = enemyStates,
            Towers = towerStates,
            Bullets = bulletStates
        };
    }

    public static void ApplySnapshot(GameManager gameManager, StateSnapshot snapshot)
    {
        if (snapshot == null) return;

        ClearWorld(gameManager);
        ApplyGlobal(gameManager, snapshot.Global);
        ApplyTowers(gameManager, snapshot.Towers);
        ApplyEnemies(gameManager, snapshot.Enemies);
        ApplyBullets(gameManager, snapshot.Bullets);
    }

    public static void ApplyFrameDelta(GameManager gameManager, FrameDelta delta)
    {
        if (delta == null) return;
        ApplyGlobal(gameManager, delta.Global);
        ApplyEvents(gameManager, delta.Events);
        ApplyEnemyTicks(gameManager, delta.Enemies);
        ApplyBulletTicks(gameManager, delta.Bullets);
    }

    private static void ClearWorld(GameManager gameManager)
    {
        gameManager.TowerController.towers.Clear();
        gameManager.EnemyController.Enemies.Clear();
        gameManager.DamageDealerController.DamageDealers.Clear();
        foreach (var zone in gameManager.Map.BuildZones)
        {
            zone.Free();
        }
    }

    private static void ApplyGlobal(GameManager gameManager, GlobalState global)
    {
        if (global == null) return;
        gameManager.UIManager.Money = global.Money;
        gameManager.UIManager.Lives = global.Lives;
        gameManager.UIManager.Wave = global.WaveIdx;
        if (gameManager.Map.DefensePoints.Count > 0)
        {
            gameManager.Map.DefensePoints[0].Health = global.Lives;
        }
    }

    private static void ApplyTowers(GameManager gameManager, IReadOnlyList<TowerState> towers)
    {
        if (towers == null) return;

        foreach (var state in towers)
        {
            if (!gameManager.TowerDefinitions.TryGetValue(state.BehaviorId, out var definition))
            {
                definition = gameManager.TowerDefinitions.GetValueOrDefault(state.ZoneId);
            }

            var behavior = TowerBehaviorFactory.CreateTowerBehavior(definition ?? new LevelLoader.TowerDefinition { Id = state.BehaviorId, Name = state.BehaviorId, Cost = 0, Range = 100, FireRate = 1f, Damage = 10 });
            var tower = new Tower(behavior, new Vector2(state.X, state.Y), definition)
            {
                Id = state.Id,
                ZoneId = state.ZoneId,
                Texture = gameManager.DefaultTowerTexture,
                UpgradeLevel = state.Level,
                cooldownTimer = state.Cooldown
            };
            tower.ApplyLevelStats();
            gameManager.TowerController.AddTower(tower);
            if (!string.IsNullOrWhiteSpace(state.ZoneId))
            {
                var zone = gameManager.Map.BuildZones.Find(z => z.Id == state.ZoneId);
                zone?.Occupy(tower);
            }
        }
    }

    private static void ApplyEnemies(GameManager gameManager, IReadOnlyList<EnemyState> enemies)
    {
        if (enemies == null) return;

        foreach (var state in enemies)
        {
            var path = gameManager.Map.GetPathById(state.PathId);
            if (path == null) continue;

            var type = EnemyRegistry.create(state.TypeId);
            var enemy = new Enemy(type, new Vector2(state.X, state.Y), path)
            {
                Id = state.Id,
                TypeId = state.TypeId,
                PathId = state.PathId,
                Status = state.Status
            };
            enemy.CurrentHealth = state.Hp;
            if (enemy.CurrentHealth <= 0)
            {
                enemy.isAlive = false;
            }
            gameManager.EnemyController.AddEnemy(enemy);
        }
    }

    private static void ApplyBullets(GameManager gameManager, IReadOnlyList<BulletState> bullets)
    {
        if (bullets == null) return;

        foreach (var state in bullets)
        {
            IDamageDealerBehavior behavior = null;
            try
            {
                behavior = DamageDealerRegistry.create(state.BehaviorId);
            }
            catch
            {
                behavior = new BulletRelated.Behaviors.StandardBulletBehavior(state.Dmg, state.Speed, state.MaxDist, state.HitRadius);
            }

            var bullet = new DamageDealer(behavior, new Vector2(state.X, state.Y), new Vector2(state.Dx, state.Dy), state.HitRadius)
            {
                Id = state.Id,
                BehaviorId = state.BehaviorId,
                Elapsed = state.Elapsed,
                TargetId = state.TargetId
            };
            gameManager.DamageDealerController.AddDamageDealer(bullet);
        }
    }

    private static void ApplyEvents(GameManager gameManager, IReadOnlyList<GameEvent> events)
    {
        if (events == null) return;

        foreach (var gameEvent in events)
        {
            switch (gameEvent)
            {
                case TowerPlacedEvent placeEvent:
                    ApplyTowerPlaced(gameManager, placeEvent);
                    break;
                case TowerRemovedEvent removedEvent:
                    ApplyTowerRemoved(gameManager, removedEvent);
                    break;
                case TowerUpgradedEvent upgradedEvent:
                    ApplyTowerUpgraded(gameManager, upgradedEvent);
                    break;
                case WaveStartedEvent waveStartedEvent:
                    gameManager.UIManager.Wave = waveStartedEvent.WaveIdx;
                    break;
                case GameOverEvent _:
                    break;
            }
        }
    }

    private static void ApplyTowerPlaced(GameManager gameManager, TowerPlacedEvent placeEvent)
    {
        var definition = gameManager.TowerDefinitions.GetValueOrDefault(placeEvent.BehaviorId);
        if (definition == null)
        {
            definition = gameManager.TowerDefinitions.Values.FirstOrDefault(d => d.Id == placeEvent.BehaviorId);
        }

        if (definition == null)
            return;

        var zone = gameManager.Map.BuildZones.Find(z => z.Id == placeEvent.ZoneId);
        if (zone == null)
            return;

        var behavior = TowerBehaviorFactory.CreateTowerBehavior(definition);
        var tower = new Tower(behavior, zone.Position, definition)
        {
            Id = placeEvent.TowerId,
            ZoneId = placeEvent.ZoneId,
            Texture = gameManager.DefaultTowerTexture
        };
        tower.ApplyLevelStats();
        gameManager.TowerController.AddTower(tower);
        zone.Occupy(tower);
    }

    private static void ApplyTowerRemoved(GameManager gameManager, TowerRemovedEvent removedEvent)
    {
        var tower = gameManager.TowerController.towers.Find(t => t.Id == removedEvent.TowerId);
        if (tower == null) return;
        gameManager.TowerController.towers.Remove(tower);
        var zone = gameManager.Map.BuildZones.Find(z => z.Id == removedEvent.ZoneId);
        zone?.Free();
    }

    private static void ApplyTowerUpgraded(GameManager gameManager, TowerUpgradedEvent upgradedEvent)
    {
        var tower = gameManager.TowerController.towers.Find(t => t.Id == upgradedEvent.TowerId);
        if (tower == null) return;

        if (!gameManager.TowerDefinitions.TryGetValue(upgradedEvent.BehaviorId, out var targetDefinition))
            return;

        var newBehavior = TowerBehaviorFactory.CreateTowerBehavior(targetDefinition);
        var upgradedTower = new Tower(newBehavior, tower.Position, targetDefinition)
        {
            Id = tower.Id,
            ZoneId = tower.ZoneId,
            Texture = tower.Texture,
            UpgradeLevel = upgradedEvent.Level
        };
        upgradedTower.ApplyLevelStats();

        int index = gameManager.TowerController.towers.IndexOf(tower);
        if (index >= 0)
        {
            gameManager.TowerController.towers[index] = upgradedTower;
        }
    }

    private static void ApplyEnemyTicks(GameManager gameManager, IReadOnlyList<EnemyTick> enemyTicks)
    {
        if (enemyTicks == null) return;

        foreach (var tick in enemyTicks)
        {
            var enemy = gameManager.EnemyController.Enemies.Find(e => e.Id == tick.Id);
            if (enemy == null) continue;
            enemy.Position = new Vector2(tick.X, tick.Y);
            enemy.CurrentHealth = tick.Hp;
            enemy.Status = tick.Status;
            if (tick.Hp <= 0)
            {
                enemy.isAlive = false;
                enemy.isKilled = true;
            }
        }
    }

    private static void ApplyBulletTicks(GameManager gameManager, IReadOnlyList<BulletTick> bulletTicks)
    {
        if (bulletTicks == null) return;

        foreach (var tick in bulletTicks)
        {
            var bullet = gameManager.DamageDealerController.DamageDealers.Find(b => b.Id == tick.Id);
            if (bullet == null) continue;
            bullet.Position = new Vector2(tick.X, tick.Y);
        }
    }

    private static BulletBehavior MapBehaviorType(string behaviorId)
    {
        if (string.IsNullOrWhiteSpace(behaviorId)) return BulletBehavior.Linear;
        if (behaviorId.ToLower().Contains("homing")) return BulletBehavior.Homing;
        if (behaviorId.ToLower().Contains("piercing")) return BulletBehavior.Piercing;
        if (behaviorId.ToLower().Contains("area")) return BulletBehavior.Area;
        return BulletBehavior.Linear;
    }

    private static float GetBulletMaxDist(DamageDealer bullet)
    {
        return 0f;
    }
}
