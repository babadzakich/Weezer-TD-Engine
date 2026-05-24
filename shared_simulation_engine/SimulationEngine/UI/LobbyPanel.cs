using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimulationEngine.UI;

public class LobbyPanel
{
    public class PlayerInfo
    {
        public string Name { get; set; }
        public bool IsHost { get; set; }
        public int Ping { get; set; }
        public float DownloadProgress { get; set; } // 0.0 to 1.0
        public bool IsReady { get; set; }
    }

    private List<PlayerInfo> _players = new();
    private Button _leaveButton;
    private Button _startButton;
    private Button _readyButton;
    private int _screenWidth;
    private int _screenHeight;
    private string _lobbyName;
    private int _maxPlayers = 4;

    public event Action OnLeaveClicked;
    public event Action OnStartClicked;

    public LobbyPanel(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _leaveButton = new Button(new Vector2(50, _screenHeight - 100), new Vector2(200, 50), "Выйти");
        _startButton = new Button(new Vector2(_screenWidth - 250, _screenHeight - 100), new Vector2(200, 50), "Начать");
        _readyButton = new Button(new Vector2(_screenWidth / 2 - 100, _screenHeight - 100), new Vector2(200, 50), "Готов");

        _leaveButton.OnClick += () => OnLeaveClicked?.Invoke();
        _startButton.OnClick += () => OnStartClicked?.Invoke();
        _readyButton.OnClick += ToggleReady;

        // Тестовые данные
        LoadMockLobby("Super Tower Defense", 4);
    }

    private void ToggleReady()
    {
        // В реальности здесь будет отправка пакета на сервер
        var me = _players.Find(p => p.Name == "HostPlayer"); // Предположим, мы "HostPlayer" для теста
        if (me != null && me.DownloadProgress >= 1.0f)
        {
            me.IsReady = !me.IsReady;
            Console.WriteLine($"Ready status toggled: {me.IsReady}");
        }
    }

    public void LoadMockLobby(string name, int maxPlayers)
    {
        _lobbyName = name;
        _maxPlayers = maxPlayers;
        _players.Clear();
        _players.Add(new PlayerInfo { Name = "HostPlayer", IsHost = true, Ping = 0, DownloadProgress = 1.0f, IsReady = false });
        _players.Add(new PlayerInfo { Name = "Guest_1", IsHost = false, Ping = 45, DownloadProgress = 0.75f, IsReady = false });
        _players.Add(new PlayerInfo { Name = "Guest_2", IsHost = false, Ping = 62, DownloadProgress = 0.30f, IsReady = false });
        
        UpdateButtonsVisibility();
    }

    private void UpdateButtonsVisibility()
    {
        // Только хост может начать игру
        var me = _players.Find(p => p.Name == "HostPlayer"); 
        bool isHost = me != null && me.IsHost;
        
        _startButton.IsVisible = isHost;
        
        // Кнопка готовности видна всем, но активна только после загрузки
        _readyButton.IsEnabled = me != null && me.DownloadProgress >= 1.0f;
        
        // Хост может начать только если все готовы (кроме него самого, или включая его)
        if (_startButton.IsVisible)
        {
            bool allReady = _players.TrueForAll(p => p.IsReady || p.IsHost); // Упрощенно: все гости готовы
            _startButton.IsEnabled = allReady;
        }
    }

    public void HandleHostLeft()
    {
        // ЗАГЛУШКА: Логика передачи хоста при выходе текущего хоста
        // Placeholder for host migration logic
        
        var currentHostIndex = _players.FindIndex(p => p.IsHost);
        if (currentHostIndex != -1)
        {
            _players.RemoveAt(currentHostIndex);
            
            // Передаем звездочку следующему
            if (_players.Count > 0)
            {
                _players[0].IsHost = true;
                _players[0].IsReady = false; // Хост обычно не нажимает "Готов", он нажимает "Начать"
                Console.WriteLine($"Host star transferred to {_players[0].Name}");
            }
        }
        UpdateButtonsVisibility();
    }

    public void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        _leaveButton.Update(gameTime, mouseState, previousMouseState);
        _startButton.Update(gameTime, mouseState, previousMouseState);
        _readyButton.Update(gameTime, mouseState, previousMouseState);

        // Имитация прогресса загрузки
        foreach (var player in _players)
        {
            if (player.DownloadProgress < 1.0f)
            {
                player.DownloadProgress += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.1f;
                if (player.DownloadProgress > 1.0f) 
                {
                    player.DownloadProgress = 1.0f;
                }
            }
        }

        UpdateButtonsVisibility();
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (font == null) return;

        string title = $"ЛОББИ: {_lobbyName}";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 80), Color.Yellow);

        // Информация о слотах (заблокирована после создания)
        string slotsInfo = $"Места: {_players.Count}/{_maxPlayers}";
        Vector2 slotsPos = new Vector2(_screenWidth - 300, 140);
        spriteBatch.DrawString(font, slotsInfo, slotsPos, Color.White);

        // Заголовки
        int startY = 180;
        spriteBatch.DrawString(font, "Игрок", new Vector2(_screenWidth / 2 - 400, startY), Color.LightGray);
        spriteBatch.DrawString(font, "Статус", new Vector2(_screenWidth / 2 - 50, startY), Color.LightGray);
        spriteBatch.DrawString(font, "Пинг", new Vector2(_screenWidth / 2 + 100, startY), Color.LightGray);
        spriteBatch.DrawString(font, "Загрузка", new Vector2(_screenWidth / 2 + 250, startY), Color.LightGray);

        startY = 230;
        for (int i = 0; i < _players.Count; i++)
        {
            var player = _players[i];
            string displayName = (player.IsHost ? "[*] " : "    ") + player.Name;
            Color nameColor = player.IsHost ? Color.Gold : Color.White;

            spriteBatch.DrawString(font, displayName, new Vector2(_screenWidth / 2 - 400, startY + i * 60), nameColor);
            
            // Отображение статуса готовности
            string statusText = player.IsHost ? "HOST" : (player.IsReady ? "ГОТОВ" : "ЖДЕТ");
            Color statusColor = player.IsHost ? Color.Gold : (player.IsReady ? Color.LimeGreen : Color.LightGray);
            spriteBatch.DrawString(font, statusText, new Vector2(_screenWidth / 2 - 50, startY + i * 60), statusColor);

            if (!player.IsHost)
            {
                spriteBatch.DrawString(font, $"{player.Ping}ms", new Vector2(_screenWidth / 2 + 100, startY + i * 60), Color.White);
            }

            // Шкала загрузки
            Rectangle barBg = new Rectangle((int)_screenWidth / 2 + 250, (int)startY + i * 60 + 10, 150, 20);
            spriteBatch.Draw(pixel, barBg, Color.DarkSlateGray);
            
            Rectangle barFill = new Rectangle(barBg.X, barBg.Y, (int)(barBg.Width * player.DownloadProgress), barBg.Height);
            spriteBatch.Draw(pixel, barFill, Color.LimeGreen);
            
            spriteBatch.DrawString(font, $"{(int)(player.DownloadProgress * 100)}%", new Vector2(barBg.X + 160, barBg.Y - 5), Color.White);
        }

        _leaveButton.Draw(spriteBatch, pixel, font);
        _startButton.Draw(spriteBatch, pixel, font);
        _readyButton.Draw(spriteBatch, pixel, font);
    }
}
