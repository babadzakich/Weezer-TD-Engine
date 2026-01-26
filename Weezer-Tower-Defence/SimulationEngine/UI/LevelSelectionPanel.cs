using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimulationEngine.UI;

public class LevelSelectionPanel
{
    private List<string> _levels = new();
    private int _selectedIndex = 0;
    private bool _isOpen = true;
    public bool IsOpen => _isOpen;

    public event Action<string> OnLevelSelected;

    public LevelSelectionPanel()
    {
        RefreshLevelList();
    }

    public void RefreshLevelList()
    {
        _isOpen = true; // Открываем панель при обновлении списка
        _levels.Clear();
        if (Directory.Exists("Content"))
        {
            var files = Directory.GetFiles("Content", "*.zip");
            foreach (var file in files)
            {
                _levels.Add(file);
            }
        }
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
        if (ks.IsKeyDown(Keys.Enter) && prevKs.IsKeyUp(Keys.Enter))
        {
            _isOpen = false;
            OnLevelSelected?.Invoke(_levels[_selectedIndex]);
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, int screenWidth, int screenHeight)
    {
        if (!_isOpen) return;

        int panelWidth = 400;
        int panelHeight = 300;
        Rectangle rect = new Rectangle((screenWidth - panelWidth) / 2, (screenHeight - panelHeight) / 2, panelWidth, panelHeight);

        sb.Draw(pixel, rect, Color.Black * 0.8f);
        sb.DrawString(font, "Select Level:", new Vector2(rect.X + 20, rect.Y + 20), Color.Yellow);

        if (_levels.Count == 0)
        {
            sb.DrawString(font, "No levels found in Content/", new Vector2(rect.X + 20, rect.Y + 60), Color.Red);
            sb.DrawString(font, "Create a level in Editor and press Ctrl+P", new Vector2(rect.X + 20, rect.Y + 90), Color.White);
            return;
        }

        for (int i = 0; i < _levels.Count; i++)
        {
            Color color = (i == _selectedIndex) ? Color.Cyan : Color.White;
            string levelName = Path.GetFileName(_levels[i]);
            sb.DrawString(font, (i == _selectedIndex ? "> " : "  ") + levelName, new Vector2(rect.X + 20, rect.Y + 60 + i * 30), color);
        }

        sb.DrawString(font, "Use Arrow Keys and Enter to select", new Vector2(rect.X + 20, rect.Height + rect.Y - 40), Color.Gray);
    }
}
