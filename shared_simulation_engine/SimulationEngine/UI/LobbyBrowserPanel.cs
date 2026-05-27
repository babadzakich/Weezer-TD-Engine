using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.Network;
using SimulationEngine.Persistence;

namespace SimulationEngine.UI;

public class LobbyBrowserPanel
{
    public class LobbyInfo
    {
        public string LobbyId { get; set; }
        public string Name { get; set; }
        public int Ping { get; set; }
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
    }

    private readonly ILobbyDiscovery _discoveryService;
    private List<LobbyInfo> _lobbies = new();
    private Button _backButton;
    private Button _refreshButton;
    private Button _probeButton;
    private TextInput _ipInput;
    private int _screenWidth;
    private int _screenHeight;
    private int _selectedIndex = -1;

    public event Action OnBackClicked;
    public event Action<LobbyInfo> OnLobbySelected;

    public LobbyBrowserPanel(int screenWidth, int screenHeight, ILobbyDiscovery discoveryService)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _discoveryService = discoveryService ?? throw new ArgumentNullException(nameof(discoveryService));

        _backButton = new Button(new Vector2(50, _screenHeight - 100), new Vector2(200, 50), "Назад");
        _refreshButton = new Button(new Vector2(_screenWidth - 250, _screenHeight - 100), new Vector2(200, 50), "Обновить");

        _ipInput = new TextInput(new Vector2(_screenWidth / 2 - 230, _screenHeight - 170), new Vector2(280, 40), "IP хоста")
        {
            AllowDigitsAndDots = true,
            MaxLength = 15
        };
        _probeButton = new Button(new Vector2(_screenWidth / 2 + 60, _screenHeight - 170), new Vector2(220, 40), "Подключиться по IP");

        _backButton.OnClick += () => OnBackClicked?.Invoke();
        _refreshButton.OnClick += RefreshLobbies;
        _probeButton.OnClick += ProbeManualIp;

        RefreshLobbies();
    }

    private void ProbeManualIp()
    {
        var ip = _ipInput.Text;
        if (string.IsNullOrWhiteSpace(ip)) return;
        _discoveryService.ProbeHost(ip);
        // Дадим хосту ~600мс на ответ, затем обновим список
        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(600);
            RefreshLobbies();
        });
    }

    public void RefreshLobbies()
    {
        _lobbies.Clear();
        _selectedIndex = -1;

        var discovered = _discoveryService.GetAvailableLobbies();
        foreach (var lobby in discovered)
        {
            _lobbies.Add(new LobbyInfo
            {
                LobbyId = lobby.LobbyId,
                Name = lobby.Name,
                Ping = lobby.Ping,
                CurrentPlayers = lobby.CurrentPlayers,
                MaxPlayers = lobby.MaxPlayers
            });
        }
    }

    public void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        _backButton.Update(gameTime, mouseState, previousMouseState);
        _refreshButton.Update(gameTime, mouseState, previousMouseState);
        _probeButton.Update(gameTime, mouseState, previousMouseState);
        _ipInput.Update(gameTime, mouseState, previousMouseState);

        // Логика выбора лобби кликом
        if (mouseState.LeftButton == ButtonState.Released && previousMouseState.LeftButton == ButtonState.Pressed)
        {
            int startY = 250;
            for (int i = 0; i < _lobbies.Count; i++)
            {
                Rectangle rect = new Rectangle(_screenWidth / 2 - 400, startY + i * 50, 800, 40);
                if (rect.Contains(mouseState.X, mouseState.Y))
                {
                    _selectedIndex = i;
                    OnLobbySelected?.Invoke(_lobbies[i]);
                    break;
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (font == null) return;

        string title = "ДОСТУПНЫЕ ЛОББИ";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 100), Color.Yellow);

        // Отрисовка заголовков таблицы
        int startY = 200;
        spriteBatch.DrawString(font, "Название", new Vector2(_screenWidth / 2 - 400, startY), Color.LightGray);
        spriteBatch.DrawString(font, "Пинг", new Vector2(_screenWidth / 2 + 100, startY), Color.LightGray);
        spriteBatch.DrawString(font, "Места", new Vector2(_screenWidth / 2 + 250, startY), Color.LightGray);

        startY = 250;
        if (_lobbies.Count == 0)
        {
            string emptyMessage = "Нет доступных лобби";
            Vector2 emptySize = font.MeasureString(emptyMessage);
            spriteBatch.DrawString(font, emptyMessage, new Vector2(_screenWidth / 2 - emptySize.X / 2, startY), Color.LightGray);
        }
        else
        {
            for (int i = 0; i < _lobbies.Count; i++)
            {
                var lobby = _lobbies[i];
                Color color = (i == _selectedIndex) ? Color.Cyan : Color.White;

                spriteBatch.DrawString(font, lobby.Name, new Vector2(_screenWidth / 2 - 400, startY + i * 50), color);
                spriteBatch.DrawString(font, $"{lobby.Ping}ms", new Vector2(_screenWidth / 2 + 100, startY + i * 50), color);
                spriteBatch.DrawString(font, $"{lobby.CurrentPlayers}/{lobby.MaxPlayers}", new Vector2(_screenWidth / 2 + 250, startY + i * 50), color);
                
                // Разделительная линия
                spriteBatch.Draw(pixel, new Rectangle(_screenWidth / 2 - 400, startY + i * 50 + 40, 800, 1), Color.Gray * 0.5f);
            }
        }

        _ipInput.Draw(spriteBatch, pixel, font);
        _probeButton.Draw(spriteBatch, pixel, font);
        _backButton.Draw(spriteBatch, pixel, font);
        _refreshButton.Draw(spriteBatch, pixel, font);
    }
}
