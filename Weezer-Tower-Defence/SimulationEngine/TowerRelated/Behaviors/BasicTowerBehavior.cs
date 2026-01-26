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
        if (_currentTarget != null)
        {
            if (Vector2.Distance(_currentTarget.Position, tower.Position) > Range || !_currentTarget.isAlive) 
                _currentTarget = null;
        }
        
        if (_currentTarget == null)
        {
            Enemy furthestEnemy = null;
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
                        furthestEnemy = enemy;
                    }
                }
            }
            _currentTarget = furthestEnemy;
        }

        if (_currentTarget == null) return null;

        // Расчет упреждения
        Vector2 relativePos = _currentTarget.Position - tower.Position;
        Vector2 targetVelocity = _currentTarget.Velocity;
        float projectileSpeed = projectileConfig.Speed;

        if (targetVelocity == Vector2.Zero || projectileSpeed <= 0)
            return _currentTarget.Position;

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
                return _currentTarget.Position + targetVelocity * t;
            }
        }
        
        return _currentTarget.Position;
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
        if (texture == null) texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
        // Рисуем радиус атаки
        DrawRangeCircle(spriteBatch, tower.Position, Range, new Color(255, 255, 255, 50));
        
        // Рисуем башню
        spriteBatch.Draw(texture, tower.Position, null, Color.White, 0f,
            new Vector2(texture.Width / 2f, texture.Height / 2f), 1f, SpriteEffects.None, 0f);
    }
    
    private void DrawRangeCircle(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        int segments = 64;
        Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (float)(2 * Math.PI * i / segments);
            float angle2 = (float)(2 * Math.PI * (i + 1) / segments);
            
            Vector2 p1 = center + new Vector2((float)Math.Cos(angle1), (float)Math.Sin(angle1)) * radius;
            Vector2 p2 = center + new Vector2((float)Math.Cos(angle2), (float)Math.Sin(angle2)) * radius;
            
            DrawLine(spriteBatch, pixel, p1, p2, color, 2f);
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

