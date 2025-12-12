using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.EnemyRelated;

namespace SimulationEngine.TowerRelated.Behaviors;

/// <summary>
/// Лазерная башня - непрерывный луч урона
/// </summary>
public class LaserTowerBehavior : ITowerBehavior
{
    // Свойства башни
    public string Id => "laser_tower";
    public string Name => "Laser Tower";
    public int Cost => 300;
    public float Range { get; }
    public float FireRate { get; }
    
    private Vector2? currentTarget;
    private IDamageDealerBehavior laserDamageBehavior;

    public LaserTowerBehavior()
    {
        Range = 180f;
        FireRate = 10f;
    }

    public LaserTowerBehavior(float range, float firerate)
    {
        Range = range;
        FireRate = firerate;
    }

    public void Update(Tower tower, GameTime gameTime)
    {
        // Лазер не имеет cooldown, постоянно наносит урон
        currentTarget = FindTarget(tower, EnemyController.GetInstance(null));
        
        if (currentTarget.HasValue)
        {
            
        }
    }

    public Vector2? FindTarget(Tower tower, EnemyController enemies)
    {
        // Ищем первого врага в радиусе
        return null;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        // Лазер не "стреляет", он наносит урон постоянно
    }
    
    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Рисуем башню и луч если есть цель
        if (currentTarget.HasValue)
        {
            // TODO: нарисовать линию от башни до цели
        }
    }
}