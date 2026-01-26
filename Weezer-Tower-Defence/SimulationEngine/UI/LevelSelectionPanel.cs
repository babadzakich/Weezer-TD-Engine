using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimulationEngine.UI;

public class LevelSelectionPanel
{
    private List<string> _levels = new();
    private int _selectedIndex = 0;
    private bool _isOpen = true;
    private SpriteFont _font;
    private Texture2D _pixel;
    private int _screenWidth;
    private int _screenHeight;

    public event Action<string> OnLevelSelected;

    public bool IsOpen => _isOpen;

    public LevelSelectionPanel(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void LoadContent(SpriteFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        RefreshLevelList();
    }

    public void RefreshLevelList()
    {
        _levels.Clear();
        string contentPath = "Content";
        if (Directory.Exists(contentPath))
        {
            var files = Directory.GetFiles(contentPath, "*.zip");
            foreach (var file in files)
            {
                _levels.Add(Path.GetFileName(file));
            }
        }
        
        if (_levels.Count > 0)
        {
            _selectedIndex = Math.Clamp(_selectedIndex, 0, _levels.Count - 1);
        }
        _isOpen = true;
    }

    public void Update(KeyboardState ks, KeyboardState prevKs)
    {
        if (!_isOpen || _levels.Count == 0) return;

        if (ks.IsKeyDown(Keys.Up) && prevKs.IsKeyUp(Keys.Up))
        {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = _levels.Count - 1;
        }
        if (ks.IsKeyDown(Keys.Down) && prevKs.IsKeyUp(Keys.Down))
        {
            _selectedIndex++;
            if (_selectedIndex >= _levels.Count) _selectedIndex = 0;
        }
        if ((ks.IsKeyDown(Keys.Enter) && prevKs.IsKeyUp(Keys.Enter)) ||
            (ks.IsKeyDown(Keys.Space) && prevKs.IsKeyUp(Keys.Space)))
        {
            _isOpen = false;
            OnLevelSelected?.Invoke(Path.Combine("Content", _levels[_selectedIndex]));
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_isOpen) return;

        // Фон на весь экран
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, _screenWidth, _screenHeight), Color.Black * 0.8f);

        if (_font == null) return;

        string title = "SELECT LEVEL";
        Vector2 titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 100), Color.Yellow);

        int startY = 200;
        for (int i = 0; i < _levels.Count; i++)
        {
            Color color = (i == _selectedIndex) ? Color.Cyan : Color.White;
            string prefix = (i == _selectedIndex) ? "> " : "  ";
            spriteBatch.DrawString(_font, prefix + _levels[i], new Vector2(_screenWidth / 2 - 150, startY + i * 40), color);
        }

        if (_levels.Count == 0)
        {
            spriteBatch.DrawString(_font, "No levels found in Content/ folder!", new Vector2(_screenWidth / 2 - 150, startY), Color.Red);
        }

        string hint = "Use UP/DOWN to select, ENTER to start";
        Vector2 hintSize = _font.MeasureString(hint);
        spriteBatch.DrawString(_font, hint, new Vector2(_screenWidth / 2 - hintSize.X / 2, _screenHeight - 100), Color.Gray);
    }
}
