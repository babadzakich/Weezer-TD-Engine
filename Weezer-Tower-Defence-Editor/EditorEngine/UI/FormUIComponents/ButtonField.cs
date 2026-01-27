using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;

class ButtonField : IShowable
{
    private readonly int top, left, width, height;
    private readonly string label;
    private readonly Action onClick;
    private bool _prevClickState = false;

    public ButtonField(int top, int left, int width, int height, string label, Action onClick)
    {
        this.top = top;
        this.left = left;
        this.width = width;
        this.height = height;
        this.label = label;
        this.onClick = onClick;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        var Bounds = new Rectangle(this.left, this.top, this.width, this.height);
        sb.Draw(pixel, Bounds, Color.DarkSlateGray);
        sb.DrawString(font, label,
            new Vector2(Bounds.X + 6, Bounds.Y + 6), Color.White);
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        var Bounds = new Rectangle(this.left, this.top, this.width, this.height);
        bool isHover = Bounds.Contains(mouse.Position);
        bool isPressed = mouse.LeftButton == ButtonState.Pressed;

        if (isHover && isPressed && !_prevClickState)
        {
            onClick?.Invoke();
        }

        _prevClickState = isPressed;
    }

    public bool IsAnyFieldActive() => false;
}
