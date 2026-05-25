using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.Infrastructure;

namespace SimulationEngine.UI;

public class LevelSelectionPanel
{
    private List<string> _levels = new();
    private int _selectedIndex = 0;
    private int _selectedMaxPlayers = 4; // Default
    private bool _isOpen = true;
    private bool _isFailure = false;
    private bool _isMultiplayerMode = false;
    private SpriteFont _font;
    private Texture2D _pixel;
    private int _screenWidth;
    private int _screenHeight;

    public event Action<string, int> OnLevelSelectedExtended;
    public event Action<string> OnLevelSelected;

    public bool IsOpen => _isOpen;
    public bool IsFailure => _isFailure;

    public void SetMultiplayerMode(bool enabled)
    {
        _isMultiplayerMode = enabled;
    }

    public LevelSelectionPanel(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    public void LoadContent(SpriteFont font, GraphicsDevice graphicsDevice)
    {
        _font = font;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        RefreshLevelList();
    }

    public void RefreshLevelList()
    {
        _levels.Clear();

        var contentPath = PathService.LevelsDirectory;
        if (Directory.Exists(contentPath))
        {
            var files = Directory.GetFiles(contentPath, "*.zip");
            foreach (var file in files)
            {
                Console.WriteLine($"Found level file: {file}");
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

        // Выбор количества игроков в мультиплеере (клавиши Left/Right)
        if (_isMultiplayerMode)
        {
            if (ks.IsKeyDown(Keys.Left) && prevKs.IsKeyUp(Keys.Left))
            {
                _selectedMaxPlayers = Math.Max(1, _selectedMaxPlayers - 1);
            }
            if (ks.IsKeyDown(Keys.Right) && prevKs.IsKeyUp(Keys.Right))
            {
                _selectedMaxPlayers = Math.Min(10, _selectedMaxPlayers + 1);
            }
        }

        if ((ks.IsKeyDown(Keys.Enter) && prevKs.IsKeyUp(Keys.Enter)) ||
            (ks.IsKeyDown(Keys.Space) && prevKs.IsKeyUp(Keys.Space)))
        {
            _isOpen = false;

            var levelPath = PathService.GetLevelArchivePath(_levels[_selectedIndex]);
            Console.WriteLine($"Selected level: {levelPath}, Max Players: {_selectedMaxPlayers}");
            
            if (_isMultiplayerMode)
            {
                OnLevelSelectedExtended?.Invoke(levelPath, _selectedMaxPlayers);
            }
            else
            {
                OnLevelSelected?.Invoke(levelPath);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_isOpen) return;

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, _screenWidth, _screenHeight), Color.Black * 0.8f);

        if (_font == null) return;

        string title = _isMultiplayerMode ? "СОЗДАНИЕ ЛОББИ: ВЫБОР КАРТЫ" : "SELECT LEVEL";
        Vector2 titleSize = _font.MeasureString(title);
        spriteBatch.DrawString(_font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 100), Color.Yellow);

        if (_isMultiplayerMode)
        {
            string slotsText = $"КОЛИЧЕСТВО МЕСТ: < {_selectedMaxPlayers} > (Left/Right)";
            Vector2 slotsSize = _font.MeasureString(slotsText);
            spriteBatch.DrawString(_font, slotsText, new Vector2(_screenWidth / 2 - slotsSize.X / 2, 150), Color.Cyan);
        }

        int startY = 220;
        for (int i = 0; i < _levels.Count; i++)
        {
            Color color = (i == _selectedIndex) ? Color.Cyan : Color.White;
            string prefix = (i == _selectedIndex) ? "> " : "  ";
            string displayName = prefix + (_levels[i].EndsWith(".zip") ? _levels[i][..^4] : _levels[i]);
            Vector2 nameSize = _font.MeasureString(displayName);
            spriteBatch.DrawString(_font, displayName, new Vector2(_screenWidth / 2 - nameSize.X / 2, startY + i * 40), color);
        }

        if (_levels.Count == 0)
        {
            string emptyMessage = $"No levels found in {PathService.LevelsDirectory}";
            Vector2 emptyMessageSize = _font.MeasureString(emptyMessage);
            spriteBatch.DrawString(
                _font,
                emptyMessage,
                new Vector2(_screenWidth / 2 - emptyMessageSize.X / 2, startY),
                Color.Red);
        }

        string hint = _isMultiplayerMode ? "UP/DOWN: Карта | LEFT/RIGHT: Места | ENTER: Создать" : "Use UP/DOWN to select, ENTER to start";
        Vector2 hintSize = _font.MeasureString(hint);
        spriteBatch.DrawString(_font, hint, new Vector2(_screenWidth / 2 - hintSize.X / 2, _screenHeight - 100), Color.Gray);
    }
}
