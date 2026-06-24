using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.EnemyRelated;
using SimulationEngine;

namespace SimulationEngine.TowerRelated.Behaviors;

public class LaserTowerBehavior : ITowerBehavior
{
    public string Id { get; }
    public string Name { get; }
    public int Cost { get; }
    public float Range { get; private set; }
    public float FireRate { get; private set; }
    public LevelLoader.TowerDefinition Definition { get; set; }

    private Vector2? currentTarget;
    private static Texture2D _pixel;

    public LaserTowerBehavior(string id, string name, IDamageDealerBehavior projectileConfig, int cost, float range, float fireRate)
    {
        Id = id;
        Name = name;
        Cost = cost;
        Range = range;
        FireRate = fireRate;
    }

    public void ApplyLevel(int level)
    {
        if (Definition == null) return;
        Range = Definition.Range;
        FireRate = Definition.FireRate;
    }

    public Vector2? FindTarget(Tower tower, EnemyController enemies)
    {
        Enemy targetEnemy = null;
        float smallestDistanceToGoal = float.MaxValue;

        foreach (var enemy in enemies.Enemies)
        {
            if (!enemy.isAlive) continue;
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

        if (targetEnemy == null)
        {
            currentTarget = null;
            return null;
        }

        currentTarget = targetEnemy.Position;
        return targetEnemy.Position;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        var enemyController = GameManager.GetInstance().EnemyController;
        if (enemyController == null) return;

        Enemy closestEnemy = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in enemyController.Enemies)
        {
            if (!enemy.isAlive) continue;
            float dist = Vector2.Distance(enemy.Position, targetPosition);
            if (dist < minDistance && dist < 10f)
            {
                minDistance = dist;
                closestEnemy = enemy;
            }
        }

        if (closestEnemy != null)
        {
            float damage = Definition?.Damage > 0 ? Definition.Damage : 5f;
            closestEnemy.TakeDamage(damage, tower.OwnerInstanceId);
        }
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        DrawRangeCircle(spriteBatch, tower.Position, Range, new Color(255, 0, 0, 15));

        if (currentTarget.HasValue)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }

            DrawLine(spriteBatch, _pixel, tower.Position, currentTarget.Value, new Color(255, 30, 30, 200), 5f);
            DrawLine(spriteBatch, _pixel, tower.Position, currentTarget.Value, Color.White, 1.5f);
        }

        if (texture != null)
        {
            float scale = Math.Min(80f / texture.Width, 80f / texture.Height);
            int targetWidth = (int)(texture.Width * scale);
            int targetHeight = (int)(texture.Height * scale);

            Rectangle destRect = new Rectangle(
                (int)tower.Position.X - targetWidth / 2,
                (int)tower.Position.Y - targetHeight / 2,
                targetWidth,
                targetHeight
            );

            spriteBatch.Draw(texture, destRect, null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
        }
    }

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
            float angle1 = (float)(2 * Math.PI * i / segments);
            float angle2 = (float)(2 * Math.PI * (i + 1) / segments);

            Vector2 p1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * radius;

            DrawLine(spriteBatch, _pixel, p1, p2, color, 1f);
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)Math.Atan2(edge.Y, edge.X);

        spriteBatch.Draw(pixel,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
            null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
    }
}
