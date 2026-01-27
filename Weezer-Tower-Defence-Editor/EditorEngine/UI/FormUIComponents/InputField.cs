using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using static System.Net.Mime.MediaTypeNames;

class InputField : IShowable
{

    private readonly int top, left, height, width;
    private readonly string id, type;
    private readonly Action<string, string, string> _onUpdate;


    public string text;
    private bool IsActive;
    private KeyboardState _prevKeyboard;

    public InputField(int top, int left, int width, int height, string id, string type, Action<string, string, string> onUpdate, string defaultValue)
    {
        this.top = top;
        this.left = left;
        this.height = height;
        this.width = width;
        this.id = id;
        this.type = type;
        this._onUpdate = onUpdate;
        this.text = defaultValue;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        var Bounds = new Rectangle(left, top, width, height);
        sb.Draw(pixel, Bounds, IsActive ? Color.DarkGray : Color.Gray);
        sb.DrawString(font, text, new Vector2(Bounds.X + 4, Bounds.Y + 4), Color.White);
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        var Bounds = new Rectangle(left, top, width, height);

        if (mouse.LeftButton == ButtonState.Pressed)
            IsActive = Bounds.Contains(mouse.Position);

        if (!IsActive)
        {
            _prevKeyboard = keyboard;
            return;
        }
        var newText = text;

        foreach (var key in keyboard.GetPressedKeys())
        {
            if (_prevKeyboard.IsKeyDown(key))
                continue;

            if (key == Keys.Back && text.Length > 0)
                newText = text[..^1];
            else if (key >= Keys.A && key <= Keys.Z)
                newText += key.ToString().ToLower();
            else if (key >= Keys.D0 && key <= Keys.D9)
                newText += (key - Keys.D0).ToString();
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                newText += (key - Keys.NumPad0).ToString();
            else if (key == Keys.OemComma) 
                newText += ",";
            else if (key == Keys.OemPeriod || key == Keys.Decimal)
                newText += ".";
            else if (key == Keys.OemMinus || key == Keys.Subtract)
                newText += "-";
            else if (key == Keys.Space)
                newText += " ";
        }
        _prevKeyboard = keyboard;

        if (type == "int")
        {
            if (!int.TryParse(newText, out _))
                return;

        }
        else if (type == "float")
        {
            if (!float.TryParse(newText + "0", out _))
                return;
        }

        text = newText;
        _onUpdate?.Invoke(text, id, type);


    }

    public bool IsAnyFieldActive() => IsActive;
}

