using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.Network;

namespace SimulationEngine.UI;

public class LobbyPanel
{
    public class PlayerInfo
    {
        public string InstanceId { get; set; }
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
    private string _lobbyName = string.Empty;
    private int _maxPlayers = 4;
    private string _localPlayerInstanceId = string.Empty;
    private LocalLobbyDiscovery _discoveryService;

    public event Action OnLeaveClicked;
    public event Action OnStartClicked;

    public LobbyPanel(int screenWidth, int screenHeight, LocalLobbyDiscovery discoveryService = null)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _discoveryService = discoveryService;

        _leaveButton = new Button(new Vector2(50, _screenHeight - 100), new Vector2(200, 50), "Выйти");
        _startButton = new Button(new Vector2(_screenWidth - 250, _screenHeight - 100), new Vector2(200, 50), "Начать");
        _readyButton = new Button(new Vector2(_screenWidth / 2 - 100, _screenHeight - 100), new Vector2(200, 50), "Готов");

        _leaveButton.OnClick += () => OnLeaveClicked?.Invoke();
        _startButton.OnClick += () => OnStartClicked?.Invoke();
        _readyButton.OnClick += ToggleReady;
    }

    private void ToggleReady()
    {
        var me = _players.Find(p => p.InstanceId == _localPlayerInstanceId);
        if (me != null && me.DownloadProgress >= 1.0f)
        {
            me.IsReady = !me.IsReady;
            _discoveryService?.UpdatePlayerStatus(me.IsReady);
            Console.WriteLine($"Ready status toggled: {me.IsReady}");
        }
    }

    public void LoadLobby(string name, int maxPlayers, IEnumerable<LocalLobbyPlayerInfo> players, string localPlayerInstanceId, LocalLobbyDiscovery discoveryService = null)
    {
        _lobbyName = name;
        _maxPlayers = maxPlayers;
        _localPlayerInstanceId = localPlayerInstanceId;
        if (discoveryService != null) _discoveryService = discoveryService;
        _players.Clear();

        foreach (var player in players)
        {
            _players.Add(new PlayerInfo
            {
                InstanceId = player.InstanceId,
                Name = player.PlayerName,
                IsHost = player.IsHost,
                Ping = player.Ping,
                DownloadProgress = 1.0f,
                IsReady = false
            });
        }

        UpdateButtonsVisibility();
    }

    private void UpdateButtonsVisibility()
    {
        var me = _players.Find(p => p.InstanceId == _localPlayerInstanceId);
        bool isHost = me != null && me.IsHost;

        _startButton.IsVisible = isHost;
        _startButton.IsEnabled = isHost && _players.Count >= 2;
        _readyButton.IsEnabled = me != null;
    }

    public void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        _leaveButton.Update(gameTime, mouseState, previousMouseState);
        _startButton.Update(gameTime, mouseState, previousMouseState);
        _readyButton.Update(gameTime, mouseState, previousMouseState);

        UpdateButtonsVisibility();
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (font == null) return;

        string title = string.IsNullOrEmpty(_lobbyName) ? "ЛОББИ" : $"ЛОББИ: {_lobbyName}";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 80), Color.Yellow);

        string slotsInfo = $"Места: {_players.Count}/{_maxPlayers}";
        Vector2 slotsPos = new Vector2(_screenWidth - 300, 140);
        spriteBatch.DrawString(font, slotsInfo, slotsPos, Color.White);

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
            Color nameColor = (player.InstanceId == _localPlayerInstanceId) ? Color.Cyan : (player.IsHost ? Color.Gold : Color.White);

            spriteBatch.DrawString(font, displayName, new Vector2(_screenWidth / 2 - 400, startY + i * 60), nameColor);

            string statusText = player.IsHost ? "HOST" : (player.IsReady ? "ГОТОВ" : "ЖДЕТ");
            Color statusColor = player.IsHost ? Color.Gold : (player.IsReady ? Color.LimeGreen : Color.LightGray);
            spriteBatch.DrawString(font, statusText, new Vector2(_screenWidth / 2 - 50, startY + i * 60), statusColor);

            if (!player.IsHost)
            {
                spriteBatch.DrawString(font, $"{player.Ping}ms", new Vector2(_screenWidth / 2 + 100, startY + i * 60), Color.White);
            }

            Rectangle barBg = new Rectangle(_screenWidth / 2 + 250, startY + i * 60 + 10, 150, 20);
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
