using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.EnemyRelated;

namespace SimulationEngine.TowerRelated.Behaviors;

/// <summary>
/// Поведение башни, основанное на TowerDefinition и уровне апгрейда.
/// </summary>
public class DefinitionTowerBehavior : ITowerBehavior
{
    private LevelLoader.TowerDefinition _definition;
    private readonly IDamageDealerBehavior _projectileConfig;

    private float _range;
    private float _fireRate;
    private float _damage;

    public string Id => _definition?.Id ?? "";
    public string Name => _definition?.Name ?? "";
    public int Cost => _definition?.Cost ?? 0;
    public float Range => _range;
    public float FireRate => _fireRate;
    public float Damage => _damage;
    
    public LevelLoader.TowerDefinition Definition 
    { 
        get => _definition; 
        set { _definition = value; if (_definition != null) ApplyLevel(0); }
    }

    public DefinitionTowerBehavior(LevelLoader.TowerDefinition definition, IDamageDealerBehavior projectileConfig)
    {
        _definition = definition;
        _projectileConfig = projectileConfig;
        ApplyLevel(0);
    }

    // Дополнительный конструктор для совместимости с TowerBehaviorFactory
    public DefinitionTowerBehavior(string id, string name, IDamageDealerBehavior projectileConfig, int cost, float range, float fireRate)
    {
        _projectileConfig = projectileConfig;
        // Мы не можем создать полноценный Definition здесь без самого объекта Definition,
        // но TowerBehaviorFactory установит свойство Definition сразу после создания.
        _range = range;
        _fireRate = fireRate;
        _damage = projectileConfig?.Damage ?? 25f;
    }

    /// <summary>
    /// Применить параметры для указанного уровня апгрейда.
    /// </summary>
    public void ApplyLevel(int level)
    {
        if (level <= 0 || _definition.UpgradeLevels == null || _definition.UpgradeLevels.Count == 0)
        {
            _range = _definition.Range;
            _fireRate = _definition.FireRate;
            _damage = _definition.Damage > 0 ? _definition.Damage : 25f;
            SyncProjectile();
            return;
        }

        int index = level - 1;
        if (index >= 0 && index < _definition.UpgradeLevels.Count)
        {
            var up = _definition.UpgradeLevels[index];
            _range = up.Range > 0 ? up.Range : _definition.Range;
            _fireRate = up.FireRate > 0 ? up.FireRate : _definition.FireRate;
            _damage = up.Damage > 0 ? up.Damage : (_definition.Damage > 0 ? _definition.Damage : 25f);
        }
        else
        {
            // если уровень выше доступного - остаемся на последнем известном
            var last = _definition.UpgradeLevels[_definition.UpgradeLevels.Count - 1];
            _range = last.Range > 0 ? last.Range : _definition.Range;
            _fireRate = last.FireRate > 0 ? last.FireRate : _definition.FireRate;
            _damage = last.Damage > 0 ? last.Damage : (_definition.Damage > 0 ? _definition.Damage : 25f);
        }
        SyncProjectile();
    }

    private void SyncProjectile()
    {
        if (_projectileConfig != null)
        {
            _projectileConfig.Damage = _damage;
        }
    }

    public Vector2? FindTarget(Tower tower, EnemyController enemies)
    {
        Enemy targetEnemy = null;
        float smallestDistanceToGoal = float.MaxValue;

        foreach (var enemy in enemies.Enemies)
        {
            float distanceToTower = Vector2.Distance(tower.Position, enemy.Position);
            if (distanceToTower <= Range)
            {
                float distanceToGoal = enemies.GetDistanceToGoal(enemy);
                if (distanceToGoal < smallestDistanceToGoal)
                {
                    smallestDistanceToGoal = distanceToGoal;
                    targetEnemy = enemy;
                }
            }
        }

        if (targetEnemy == null) return null;

        // Расчет упреждения (Lead Shooting)
        Vector2 relativePos = targetEnemy.Position - tower.Position;
        Vector2 targetVelocity = targetEnemy.Velocity;
        float projectileSpeed = _projectileConfig.Speed;

        // Если враг стоит или пуля бесконечно быстрая, стреляем прямо в него
        if (targetVelocity == Vector2.Zero || projectileSpeed <= 0)
            return targetEnemy.Position;

        // Решаем квадратное уравнение: a*t^2 + b*t + c = 0
        // a = |V|^2 - s^2
        // b = 2 * (RelativePos * V)
        // c = |RelativePos|^2
        float a = Vector2.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        float b = 2f * Vector2.Dot(relativePos, targetVelocity);
        float c = Vector2.Dot(relativePos, relativePos);

        float discriminant = b * b - 4f * a * c;

        if (discriminant >= 0)
        {
            float t1 = (-b + (float)Math.Sqrt(discriminant)) / (2f * a);
            float t2 = (-b - (float)Math.Sqrt(discriminant)) / (2f * a);

            float t = -1;
            if (t1 > 0 && t2 > 0) t = Math.Min(t1, t2);
            else if (t1 > 0) t = t1;
            else if (t2 > 0) t = t2;

            if (t > 0)
            {
                Vector2 predictedPosition = targetEnemy.Position + targetVelocity * t;
                
                // Дополнительная проверка: не ушла ли предсказанная точка за радиус атаки слишком далеко
                // (хотя технически это корректная точка перехвата, если она вообще возможна)
                return predictedPosition;
            }
        }

        return targetEnemy.Position;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        Vector2 direction = Vector2.Normalize(targetPosition - tower.Position);
        var bullet = new DamageDealer(_projectileConfig, tower.Position, direction, _projectileConfig.HitRadius);
        GameManager.GetInstance().DamageDealerController?.AddDamageDealer(bullet);
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Рисуем радиус атаки (простой круг линией)
        DrawRangeCircle(spriteBatch, tower.Position, Range, new Color(255, 255, 255, 50));

        if (texture != null)
        {
            spriteBatch.Draw(texture, tower.Position, null, Color.White, 0f,
                new Vector2(texture.Width / 2f, texture.Height / 2f), 1f, SpriteEffects.None, 0f);
        }
        else
        {
            // Fallback if no texture assigned
            // We don't have a shared pixel here, so we just don't draw the tower body
            // but the range circle will be visible. 
            // Better to ensure texture is assigned.
        }
    }

    private static Texture2D _pixel;

    private void DrawRangeCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        int segments = 48;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(2 * System.Math.PI * i / segments);
            float angle2 = (float)(2 * System.Math.PI * (i + 1) / segments);

            Vector2 p1 = center + new Vector2((float)System.Math.Cos(angle1), (float)System.Math.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2((float)System.Math.Cos(angle2), (float)System.Math.Sin(angle2)) * radius;

            DrawLine(spriteBatch, _pixel, p1, p2, color, 2f);
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)System.Math.Atan2(edge.Y, edge.X);

        spriteBatch.Draw(pixel,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
            null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
    }
}

