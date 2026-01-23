using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EditorEngine.UI;


// fix: only one click per press
public class UIButton
{
    public Rectangle Bounds;
    public string Text;
    public Action OnClick;
    private bool _prevClickState = false;

    public UIButton(Rectangle bounds, string text, Action onClick)
    {
        Bounds = bounds;
        Text = text;
        OnClick = onClick;
    }

    public void Update(MouseState mouse)
    {
        bool isHover = Bounds.Contains(mouse.Position);
        bool isPressed = mouse.LeftButton == ButtonState.Pressed;

        if (isHover && isPressed && !_prevClickState)
        {
            OnClick?.Invoke();
        }

        _prevClickState = isPressed;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        sb.Draw(pixel, Bounds, Color.DarkSlateGray);
        sb.DrawString(font, Text,
            new Vector2(Bounds.X + 6, Bounds.Y + 6), Color.White);
    }
}
