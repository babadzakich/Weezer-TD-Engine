using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine;
using SimulationEngine.TowerRelated;
using SimulationEngine.EnemyRelated;
using SimulationEngine.BulletRelated;

namespace MyPlugins.Towers;

public class BasicBehavior : ITowerBehavior
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public int Cost { get; private set; }
    public float Range { get; private set; }
    public float FireRate { get; private set; }

    private IDamageDealerBehavior _projectileBehavior;
    private Enemy _currentTarget;

    // ОБЯЗАТЕЛЬНО пустой конструктор
    public BasicBehavior() { }

    // Конфигурация из JSON / runtime
    public void Configure(
        string id,
        string name,
        IDamageDealerBehavior projectileBehavior,
        int cost,
        float range,
        float fireRate)
    {
        Id = id;
        Name = name;
        _projectileBehavior = projectileBehavior;
        Cost = cost;
        Range = range;
        FireRate = fireRate;
    }

    public Vector2? FindTarget(Tower tower, EnemyController enemies)
    {
        if (_currentTarget != null)
        {
            if (Vector2.Distance(_currentTarget.Position, tower.Position) <= Range)
                return _currentTarget.Position;

            _currentTarget = null;
        }

        Enemy best = null;
        float bestDist = float.MaxValue;

        foreach (var enemy in enemies.Enemies)
        {
            float dist = Vector2.Distance(tower.Position, enemy.Position);
            if (dist > Range)
                continue;

            float distToGoal = GameManager
                .GetInstance()
                .EnemyController
                .GetDistanceToGoal(enemy);

            if (distToGoal < bestDist)
            {
                bestDist = distToGoal;
                best = enemy;
            }
        }

        _currentTarget = best;
        return best?.Position;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        Vector2 dir = Vector2.Normalize(targetPosition - tower.Position);
        var bullet = new DamageDealer(_projectileBehavior, tower.Position, dir);

        GameManager
            .GetInstance()
            .DamageDealerController
            ?.AddDamageDealer(bullet);
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        if (texture == null)
        {
            texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            texture.SetData(new[] { Color.White });
        }

        spriteBatch.Draw(
            texture,
            tower.Position,
            null,
            Color.White,
            0f,
            new Vector2(0.5f, 0.5f),
            10f,
            SpriteEffects.None,
            0f
        );
    }
}
