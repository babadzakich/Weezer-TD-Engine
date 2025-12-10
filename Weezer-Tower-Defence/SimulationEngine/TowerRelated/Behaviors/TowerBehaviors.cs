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
    public void Initialize(Tower tower, TowerConfig config)
    {
        // Можно загрузить дополнительные параметры из config.CustomParameters
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

/// <summary>
/// Снайперская башня - стреляет в самого далёкого врага с большой дальностью
/// </summary>
public class SniperTowerBehavior : ITowerBehavior
{
    public void Initialize(Tower tower, TowerConfig config)
    {
        // Инициализация
    }

    public void Update(Tower tower, GameTime gameTime)
    {
        // Обновление логики
    }

    public Vector2? FindTarget(Tower tower, object[] enemies)
    {
        // Логика снайпера - самый далёкий враг в радиусе
        // TODO: реализовать когда будет система врагов
        return null;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        Vector2 direction = Vector2.Normalize(targetPosition - tower.Position);
        
        // TODO: создать снайперскую пулю (пробивающую)
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Рисуем башню
    }
}

/// <summary>
/// AOE башня - стреляет в скопление врагов
/// </summary>
public class AoeTowerBehavior : ITowerBehavior
{
    private float splashRadius;
    
    public void Initialize(Tower tower, TowerConfig config)
    {
        // Читаем кастомный параметр из конфига
        if (config.CustomParameters.TryGetValue("SplashRadius", out object radiusObj))
        {
            splashRadius = Convert.ToSingle(radiusObj);
        }
        else
        {
            splashRadius = 50f; // По умолчанию
        }
    }

    public void Update(Tower tower, GameTime gameTime)
    {
        // Обновление логики
    }

    public Vector2? FindTarget(Tower tower, object[] enemies)
    {
        // Логика AOE - ищем точку с максимальным количеством врагов в радиусе
        // TODO: реализовать когда будет система врагов
        return null;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        Vector2 direction = Vector2.Normalize(targetPosition - tower.Position);
        
        // TODO: создать сплеш пулю
    }

    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Рисуем башню
    }
}

/// <summary>
/// Лазерная башня - непрерывный луч урона
/// </summary>
public class LaserTowerBehavior : ITowerBehavior
{
    private Vector2? currentTarget;
    
    public void Initialize(Tower tower, TowerConfig config)
    {
        // Инициализация
    }

    public void Update(Tower tower, GameTime gameTime)
    {
        // Лазер не имеет cooldown, постоянно наносит урон
        currentTarget = FindTarget(tower, new object[0]);
        
        if (currentTarget.HasValue)
        {
            // Наносим урон каждый кадр
            DealContinuousDamage(tower, currentTarget.Value, gameTime);
        }
    }

    public Vector2? FindTarget(Tower tower, object[] enemies)
    {
        // Ищем первого врага в радиусе
        return null;
    }

    public void Fire(Tower tower, Vector2 targetPosition)
    {
        // Лазер не "стреляет", он наносит урон постоянно
    }
    
    private void DealContinuousDamage(Tower tower, Vector2 target, GameTime gameTime)
    {
        // TODO: наносить урон врагу каждый кадр
        float damagePerSecond = tower.Config.Damage * tower.Config.FireRate;
        float frameDamage = damagePerSecond * (float)gameTime.ElapsedGameTime.TotalSeconds;
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
