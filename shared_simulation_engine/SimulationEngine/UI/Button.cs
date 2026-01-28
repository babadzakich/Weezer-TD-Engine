using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace SimulationEngine.UI;

/// <summary>
/// Кнопка UI
/// </summary>
public class Button : UIElement
{
    public string Text { get; set; }
    public Color BackgroundColor { get; set; }
    public Color HoverColor { get; set; }
    public Color TextColor { get; set; }
    public Color DisabledColor { get; set; }
    
    public event Action OnClick;
    
    private bool _isHovered;

    public Button(Vector2 position, Vector2 size, string text) : base(position, size)
    {
        Text = text;
        BackgroundColor = new Color(50, 50, 50);
        HoverColor = new Color(80, 80, 80);
        TextColor = Color.White;
        DisabledColor = new Color(30, 30, 30);
    }

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        if (!IsVisible) return;

        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        _isHovered = Contains(mousePos);

        if (IsEnabled && _isHovered && 
            mouseState.LeftButton == ButtonState.Released && 
            previousMouseState.LeftButton == ButtonState.Pressed)
        {
            OnClick?.Invoke();
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible) return;

        Color bgColor = !IsEnabled ? DisabledColor : (_isHovered ? HoverColor : BackgroundColor);
        
        // Рисуем фон кнопки
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), bgColor);
        
        // Рисуем рамку
        DrawBorder(spriteBatch, pixel, Position, Size, Color.White, 2);

        // Рисуем текст по центру
        if (font != null && !string.IsNullOrEmpty(Text))
        {
            Vector2 textSize = font.MeasureString(Text);
            Vector2 textPos = Position + (Size - textSize) / 2;
            spriteBatch.DrawString(font, Text, textPos, IsEnabled ? TextColor : Color.Gray);
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position, Vector2 size, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)position.Y, (int)size.X, thickness), color); // Top
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)(position.Y + size.Y - thickness), (int)size.X, thickness), color); // Bottom
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)position.Y, thickness, (int)size.Y), color); // Left
        spriteBatch.Draw(pixel, new Rectangle((int)(position.X + size.X - thickness), (int)position.Y, thickness, (int)size.Y), color); // Right
    }
}
