using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EditorEngine.UI;

public class UITextField
{
    public Rectangle Bounds;
    public string Text = "";
    public bool IsActive;

    public UITextField(Rectangle bounds, string initial = "")
    {
        Bounds = bounds;
        Text = initial;
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        if (mouse.LeftButton == ButtonState.Pressed)
            IsActive = Bounds.Contains(mouse.Position);

        if (!IsActive) return;

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (key == Keys.Back && Text.Length > 0)
                Text = Text[..^1];
            else if (key >= Keys.A && key <= Keys.Z)
                Text += key.ToString().ToLower();
            else if (key >= Keys.D0 && key <= Keys.D9)
                Text += (key - Keys.D0).ToString();
            else if (key == Keys.OemPeriod)
                Text += ".";
            else if (key == Keys.OemMinus)
                Text += "-";
            else if (key == Keys.Space)
                Text += " ";
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        sb.Draw(pixel, Bounds, IsActive ? Color.DarkGray : Color.Gray);
    sb.DrawString(font, Text ?? "", new Vector2(Bounds.X + 4, Bounds.Y + 4), Color.White);
    }
}
