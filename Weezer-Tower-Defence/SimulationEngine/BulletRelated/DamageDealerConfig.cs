using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;

namespace SimulationEngine.BulletRelated;

/// <summary>
/// Базовый класс конфигурации для всех типов пуль.
/// Наследуйте от него для создания своих типов пуль в плагинах.
/// </summary>
public abstract class DamageDealerConfig
{
    public int DamageAmount { get; }
    public float Speed { get; }
    public string TypeName { get; }
    
    // Визуальные параметры
    public string TexturePath { get; protected set; }
    public Color TintColor { get; protected set; }
    public float Scale { get; protected set; }
    public float Rotation { get; protected set; }

    protected DamageDealerConfig(int damageAmount, float speed, string typeName, 
        string texturePath = null, Color? tintColor = null, float scale = 1f)
    {
        DamageAmount = damageAmount;
        Speed = speed;
        TypeName = typeName;
        TexturePath = texturePath;
        TintColor = tintColor ?? Color.White;
        Scale = scale;
        Rotation = 0f;
    }

    /// <summary>
    /// Переопределите этот метод для кастомной логики поведения пули
    /// </summary>
    public abstract void ApplyBehavior(DamageDealer damageDealer, GameTime deltaTime);
    
    /// <summary>
    /// Переопределите этот метод для кастомной отрисовки пули
    /// </summary>
    public virtual void Draw(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, float rotation)
    {
        if (texture != null)
        {
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            spriteBatch.Draw(texture, position, null, TintColor, rotation, origin, Scale, SpriteEffects.None, 0f);
        } else {
            spriteBatch.DrawRectangle(new Rectangle((int)position.X - 5, (int)position.Y - 5, 10, 10), TintColor);
        }
    }
}
