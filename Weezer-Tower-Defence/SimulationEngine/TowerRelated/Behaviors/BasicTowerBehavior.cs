using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using System;
using System.Linq;

namespace SimulationEngine.TowerRelated.Behaviors;

/// <summary>
/// Базовое поведение башни - стреляет в ближайшего врага
/// </summary>
public class BasicTowerBehavior : ITowerBehavior
{
    // Свойства башни
    public string Id => "basic_tower";
    public string Name => "Basic Tower";
    public int Cost => 100;
    public float Range => 150f;
    public float FireRate => 1f;

    public BasicTowerBehavior()
    {
        // Можно загрузить дополнительные параметры
    }

    public void Update(Tower tower, GameTime gameTime)
    {
        // Базовая логика обновления
    }

    public Vector2? FindTarget(Tower tower, object[] enemies)
    {
        // Базовая логика - ищем ближайшего врага в радиусе
        // TODO: когда будет система врагов, реализовать поиск
        return null;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        // Создаём пулю в направлении цели
        Vector2 direction = Vector2.Normalize(targetPosition - tower.Position);
        
        // TODO: создать DamageDealer и добавить в контроллер
        // var bullet = new DamageDealer(projectileConfig, tower.Position, direction);
        // DamageDealerController.GetInstance(null).AddDamageDealer(bullet);
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Рисуем башню
    }
}

