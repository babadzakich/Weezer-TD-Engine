using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace EditorEngine.UI;

// Added fix to prevent multiple characters being added
public class UITextField
{
    public Rectangle Bounds;
    public string Text = "";
    public bool IsActive;
    private KeyboardState _prevKeyboard;
    private Action<string, string> onUpdate;
    public readonly string id;

    public UITextField(Rectangle bounds, string initial = "", Action<string, string> onUpdate = null, string id = null)
    {
        Bounds = bounds;
        Text = initial;
        _prevKeyboard = Keyboard.GetState();
        this.onUpdate = onUpdate;
        this.id = id;
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        if (mouse.LeftButton == ButtonState.Pressed)
            IsActive = Bounds.Contains(mouse.Position);

        if (!IsActive)
        {
            _prevKeyboard = keyboard;
            return;
        }

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (_prevKeyboard.IsKeyDown(key))
                continue;

            if (key == Keys.Back && Text.Length > 0)
                Text = Text[..^1];
            else if (key >= Keys.A && key <= Keys.Z)
                Text += key.ToString().ToLower();
            else if (key >= Keys.D0 && key <= Keys.D9)
                Text += (key - Keys.D0).ToString();
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                Text += (key - Keys.NumPad0).ToString();
            else if (key == Keys.OemPeriod || key == Keys.Decimal)
                Text += ".";
            else if (key == Keys.OemMinus || key == Keys.Subtract)
                Text += "-";
            else if (key == Keys.Space)
                Text += " ";
            
            onUpdate?.Invoke(Text, id);
        }

        _prevKeyboard = keyboard;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        sb.Draw(pixel, Bounds, IsActive ? Color.DarkGray : Color.Gray);
    sb.DrawString(font, Text ?? "", new Vector2(Bounds.X + 4, Bounds.Y + 4), Color.White);
    }
}
