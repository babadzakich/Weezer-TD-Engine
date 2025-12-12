using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;

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
    private Enemy _currentTarget;

    public BasicTowerBehavior(string id, string name, IDamageDealerBehavior projectileConfig, int cost, float range, float fireRate)
    {
        Id = id;
        Name = name;
        this.projectileConfig = projectileConfig;
        Cost = cost;
        Range = range;
        FireRate = fireRate;
    }

    public void Update(Tower tower, GameTime gameTime)
    {
        
    }

    public Vector2? FindTarget(Tower tower, EnemyController enemies)
    {
        
        if (_currentTarget != null)
        {
            if (Vector2.Distance(_currentTarget.Position, tower.Position) > Range) 
                _currentTarget = null;
            else
                return _currentTarget.Position;
        }
        Enemy closestEnemy = null;
        float closestDistance = float.MaxValue;

        foreach (var enemy in enemies.Enemies)
        {
            float distance = Vector2.Distance(tower.Position, enemy.Position);
            if (distance <= Range && distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }
        
        return closestEnemy?.Position;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        // Создаём пулю в направлении цели
        Vector2 direction = Vector2.Normalize(targetPosition - tower.Position);
        
        // TODO: создать DamageDealer и добавить в контроллер
        var bullet = new DamageDealer(projectileConfig, tower.Position, direction);
        DamageDealerController.GetInstance(null).AddDamageDealer(bullet);
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Рисуем башню
    }
}

