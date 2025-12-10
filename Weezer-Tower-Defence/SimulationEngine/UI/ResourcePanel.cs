using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimulationEngine.UI;

/// <summary>
/// Панель отображения ресурсов (деньги, жизни и т.д.)
/// </summary>
public class ResourcePanel : UIElement
{
    public int Money { get; set; }
    public int Lives { get; set; }
    public int Wave { get; set; }
    
    public Color BackgroundColor { get; set; }
    public Color TextColor { get; set; }

    public ResourcePanel(Vector2 position, Vector2 size) : base(position, size)
    {
        BackgroundColor = new Color(30, 30, 30, 200);
        TextColor = Color.White;
        Money = 500;
        Lives = 20;
        Wave = 0;
    }

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        // Панель ресурсов не интерактивна
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible) return;

        // Фон панели
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);

        if (font != null)
        {
            Vector2 textPos = Position + new Vector2(10, 10);
            spriteBatch.DrawString(font, $"Money: ${Money}", textPos, Color.Gold);
            textPos.Y += 25;
            spriteBatch.DrawString(font, $"Lives: {Lives}", textPos, Color.Red);
            textPos.Y += 25;
            spriteBatch.DrawString(font, $"Wave: {Wave}", textPos, Color.Cyan);
        }
    }
}
