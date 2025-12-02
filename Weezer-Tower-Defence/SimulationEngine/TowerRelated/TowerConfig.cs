using SimulationEngine.BulletRelated;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.TowerRelated;

/// <summary>
/// Базовый класс типа башни. Наследуйте от него для создания своих башен в плагинах.
/// </summary>
public abstract class TowerConfig
{
    public string Id { get; } // уникальный идентификатор типа башни
    public int Cost { get; }
    public float Range { get; } // радиус обнаружения в пикселях
    public float FireRate { get; } // выстрелов в секунду
    public DamageDealerConfig ProjectileConfig { get; }
    
    // Визуальные параметры
    public string TexturePath { get; protected set; }
    public Color TintColor { get; protected set; }
    public float Scale { get; protected set; }

    protected TowerConfig(string id, int cost, float range, float fireRate, 
        DamageDealerConfig projectileConfig, string texturePath = null, 
        Color? tintColor = null, float scale = 1f)
    {
        Id = id;
        Cost = cost;
        Range = range;
        FireRate = fireRate;
        ProjectileConfig = projectileConfig;
        TexturePath = texturePath;
        TintColor = tintColor ?? Color.White;
        Scale = scale;
    }

    /// <summary>
    /// Переопределите для кастомного поведения башни (например, особая логика стрельбы)
    /// </summary>
    public virtual void OnFire(Tower tower, Vector2 targetPosition)
    {
        // Базовая логика стрельбы - создаём пулю
    }

    /// <summary>
    /// Переопределите для кастомной логики выбора цели
    /// </summary>
    public virtual Vector2? FindTarget(Tower tower, object[] enemies)
    {
        // Базовая логика - ближайший враг в радиусе
        return null;
    }

    /// <summary>
    /// Переопределите для кастомной отрисовки башни
    /// </summary>
    public virtual void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position)
    {
        if (texture != null)
        {
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            spriteBatch.Draw(texture, position, null, TintColor, 0f, origin, Scale, SpriteEffects.None, 0f);
        }
    }
}