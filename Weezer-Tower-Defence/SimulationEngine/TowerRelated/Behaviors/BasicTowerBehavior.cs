using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.EnemyRelated;
using SimulationEngine.BulletRelated.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using SimulationEngine;

namespace SimulationEngine.TowerRelated.Behaviors;

/// <summary>
/// Базовое поведение башни - стреляет в ближайшего врага
/// </summary>
public class BasicTowerBehavior : ITowerBehavior
{
    // Свойства башни
    public string Id { get; }
    public string Name { get; }
    public IDamageDealerBehavior projectileConfig;
    public int Cost { get; }
    public float Range { get; }
    public float FireRate { get; }
    private SimulationEngine.EnemyRelated.Enemy _currentTarget;

    public BasicTowerBehavior(string id, string name, IDamageDealerBehavior projectileConfig, int cost, float range, float fireRate)
    {
        Id = id;
        Name = name;
        this.projectileConfig = projectileConfig;
        Cost = cost;
        Range = range;
        FireRate = fireRate;
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
        float projectileSpeed = projectileConfig.Speed;

        // Если враг стоит или пуля бесконечно быстрая, стреляем прямо в него
        if (targetVelocity == Vector2.Zero || projectileSpeed <= 0)
            return targetEnemy.Position;

        // Решаем квадратное уравнение: a*t^2 + b*t + c = 0
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
                return targetEnemy.Position + targetVelocity * t;
            }
        }

        return targetEnemy.Position;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        // Создаём пулю в направлении цели
        Vector2 direction = Vector2.Normalize(targetPosition - tower.Position);
        
        var bullet = new DamageDealer(projectileConfig, tower.Position, direction, projectileConfig.HitRadius);
        GameManager.GetInstance().DamageDealerController?.AddDamageDealer(bullet);
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Рисуем радиус атаки
        DrawRangeCircle(spriteBatch, tower.Position, Range, new Color(255, 255, 255, 50));
        
        // Рисуем башню
        if (texture != null)
        {
            spriteBatch.Draw(texture, tower.Position, null, Color.White, 0f,
                new Vector2(texture.Width / 2f, texture.Height / 2f), 1f, SpriteEffects.None, 0f);
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
            float angle1 = (float)(2 * Math.PI * i / segments);
            float angle2 = (float)(2 * Math.PI * (i + 1) / segments);
            
            Vector2 p1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * radius;
            
            DrawLine(spriteBatch, _pixel, p1, p2, color, 2f);
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

