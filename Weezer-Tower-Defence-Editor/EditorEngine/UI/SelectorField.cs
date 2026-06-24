using System;
using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EditorEngine.UI;

public class SelectorField
{
    private Rectangle _bounds;
    private bool _isActive;

    private readonly List<string> _options;
    private readonly Action<string> _onClick;

    private int _selectedIndex = -1;

    private MouseState _prevMouse;

    private const int ItemHeight = 35;

    public SelectorField(int top, int left, List<string> options, Action<string> onClick)
    {
        _bounds = new Rectangle(left, top, 200, 30);
        _options = options;
        _onClick = onClick;
    }

    public void Show() => _isActive = true;
    public void Hide() => _isActive = false;

    public void Update(MouseState mouse)
    {
        if (mouse.LeftButton == ButtonState.Pressed &&
            _prevMouse.LeftButton == ButtonState.Released)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                var itemRect = GetItemRect(i);

                if (itemRect.Contains(mouse.Position))
                {
                    _selectedIndex = i;
                    _onClick?.Invoke(_options[i]);
                    break;
                }
            }
        }

        _prevMouse = mouse;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {

        for (int i = 0; i < _options.Count; i++)
        {
            var rect = GetItemRect(i);
            var color = i == _selectedIndex ? Color.DarkGray : Color.DimGray;

            sb.Draw(pixel, rect, color);
            DrawBorder(sb, pixel, rect);

            sb.DrawString(
                font,
                _options[i],
                new Vector2(rect.X + 10, rect.Y + 4),
                Color.White
            );
        }
    }

    private Rectangle GetItemRect(int index)
    {
        return new Rectangle(
            _bounds.X,
            _bounds.Bottom + index * ItemHeight,
            _bounds.Width,
            ItemHeight
        );
    }

    private void DrawBorder(SpriteBatch sb, Texture2D pixel, Rectangle r)
    {
        sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, 1), Color.Black);
        sb.Draw(pixel, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), Color.Black);
        sb.Draw(pixel, new Rectangle(r.X, r.Y, 1, r.Height), Color.Black);
        sb.Draw(pixel, new Rectangle(r.Right - 1, r.Y, 1, r.Height), Color.Black);
    }
}

