using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;

namespace SimulationEngine.TowerRelated;

/// <summary>
/// Интерфейс для поведения башни - только логика, без данных
/// </summary>
public interface ITowerBehavior
{
    /// <summary>
    /// Инициализация поведения для конкретной башни
    /// </summary>
    void Initialize(Tower tower, TowerConfig config);
    
    /// <summary>
    /// Обновление логики башни
    /// </summary>
    void Update(Tower tower, GameTime gameTime);
    
    /// <summary>
    /// Найти цель для атаки
    /// </summary>
    Vector2? FindTarget(Tower tower, object[] enemies);
    
    /// <summary>
    /// Выстрелить по цели
    /// </summary>
    void Fire(Tower tower, Vector2 targetPosition);
    
    /// <summary>
    /// Отрисовка (для кастомных эффектов, если нужно)
    /// </summary>
    void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture);
}