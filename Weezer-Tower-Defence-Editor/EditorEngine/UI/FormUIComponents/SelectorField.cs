using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

class SelectorFieldForm : IShowable
{

    public readonly int top, left, height, width;
    public readonly List<String> options;
    private readonly Action<string, string> _onClick;
    private readonly string id;

    private int _selectedIndex = -1;
    private MouseState _prevMouse;

    public SelectorFieldForm(int top, int left, int width, int height, string id, List<string> options, Action<string, string> onClick)
    {
        this.top = top;
        this.left = left;
        this.height = height;
        this.width = width;
        this.options = options;
        this.id = id;
        _onClick = onClick;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        for (int i = 0; i < options.Count; i++)
        {
            var rect = new Rectangle(
                    left,
                    top + i * height,
                    width,
                    height
                ); 
            var color = i == _selectedIndex ? Color.DarkGray : Color.DimGray;

            sb.Draw(pixel, rect, color);

            sb.DrawString(
                font,
                options[i],
                new Vector2(rect.X + 10, rect.Y + 4),
                Color.White
            );
        }
    }

    public void Update(MouseState mouse, KeyboardState keyboard) {
        if (mouse.LeftButton == ButtonState.Pressed &&
                _prevMouse.LeftButton == ButtonState.Released)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var itemRect = new Rectangle(
                    left,
                    top + i * height,
                    width,
                    height
                );

                if (itemRect.Contains(mouse.Position))
                {
                    _selectedIndex = i;
                    _onClick?.Invoke(options[i], id);
                    break;
                }
            }
        }

        _prevMouse = mouse;
    }

    public bool IsAnyFieldActive() => false;
}

