using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;

namespace SimulationEngine.TowerRelated;

/// <summary>
/// Интерфейс для поведения башни - содержит и логику, и данные
/// 
/// Для создания нового типа башни нужно:
/// 1. Создать класс, реализующий ITowerBehavior
/// 2. Определить все свойства (Id, Name, Cost, Range, FireRate, Damage)
/// 3. Реализовать методы Update, FindTarget, Fire, Draw
/// 4. Зарегистрировать в TowerBehaviorRegistry.Instance.Register(id, () => new YourTowerBehavior())
/// 
/// Пример смотрите в ExampleCustomTower.cs
/// </summary>
public interface ITowerBehavior
{
    // Свойства башни
    /// <summary>Уникальный идентификатор типа башни</summary>
    string Id { get; }
    /// <summary>Название башни для UI</summary>
    string Name { get; }
    /// <summary>Стоимость постройки</summary>
    int Cost { get; }
    /// <summary>Дальность атаки</summary>
    float Range { get; }
    /// <summary>Скорострельность (выстрелов в секунду)</summary>
    float FireRate { get; }
 
    /// <summary>
    /// Обновление логики башни
    /// </summary>
    void Update(Tower tower, GameTime gameTime);
    
    /// <summary>
    /// Найти цель для атаки
    /// </summary>
    Vector2? FindTarget(Tower tower, EnemyController enemies);
    
    /// <summary>
    /// Выстрелить по цели
    /// </summary>
    void Fire(Tower tower, Vector2 targetPosition);
    
    /// <summary>
    /// Отрисовка (для кастомных эффектов, если нужно)
    /// </summary>
    void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture);
}