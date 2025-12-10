using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimulationEngine.UI;

/// <summary>
/// Базовый класс для всех UI элементов
/// </summary>
public abstract class UIElement
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    
    protected UIElement(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
        IsVisible = true;
        IsEnabled = true;
    }

    /// <summary>
    /// Проверка, находится ли точка внутри элемента
    /// </summary>
    public virtual bool Contains(Vector2 point)
    {
        return point.X >= Position.X && point.X <= Position.X + Size.X &&
               point.Y >= Position.Y && point.Y <= Position.Y + Size.Y;
    }

    public abstract void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState);
    public abstract void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font);
}
