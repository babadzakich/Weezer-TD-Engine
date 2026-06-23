using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.BulletRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.MapRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.TowerRelated.Behaviors;
using SimulationEngine.EnemyRelated;
using SimulationEngine.WaveRelated;
using SimulationEngine.EnemyRelated.EnemyTypes;
using SimulationEngine;
using SimulationEngine.UI;
using SimulationEngine.Network;
using System.Linq;
using SimulationEngine.Infrastructure;

namespace Weezer_Tower_Defence;

public class GameRunner : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _towerTexture;
    private Texture2D _enemyTexture;
    private Texture2D _pixel;
    private Texture2D _pathTexture;
    private Texture2D _bulletTexture;
    private SpriteFont _font;

    private DamageDealerController damageDealerController;
    private TowerController towerController;
    private EnemyController enemyController;
    private WaveController waveController;
    private GameManager gameManager;
    private GameMap gameMap;

    private Matrix _scaleMatrix = Matrix.Identity;
    private const int VirtualWidth = 1600;
    private const int VirtualHeight = 900;

    private LevelSelectionPanel _levelSelectionPanel;
    private MainMenuPanel _mainMenuPanel;
    private MultiplayerMenuPanel _multiplayerMenuPanel;
    private LobbyBrowserPanel _lobbyBrowserPanel;
    private LobbyPanel _lobbyPanel;
    private ILobbyDiscovery _lobbyDiscovery;
    private GameSyncManager _gameSyncManager;
    private string? _currentLobbyId;
    private string _currentLobbyName = string.Empty;
    private string _playerName = string.Empty;
    private string _currentLobbyLevelPath = string.Empty;
    private bool _gameStartingInLobby = false;
    private int _lastObservedHostWaveStartIndex = -1;
    private DateTimeOffset _lastLobbyStateRefresh = DateTimeOffset.MinValue;
    
    private GameState _currentState = GameState.MainMenu;
    private bool _isSelectingLevelForLobby = false;
    private bool _showInstructions = false;
    private KeyboardState _previousKeyboardState;
    private MouseState _previousMouseState;

    public enum GameState
    {
        MainMenu,
        MultiplayerMenu,
        LobbyBrowser,
        Lobby,
        LevelSelection,
        Playing
    }

    public GameRunner()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = VirtualWidth,
            PreferredBackBufferHeight = VirtualHeight
        };
        var commonRoot = PathService.CommonDirectory;

        Console.WriteLine($"Common content root: {commonRoot}");
        
        Content.RootDirectory = $"{commonRoot}";
        IsMouseVisible = true;
        
        // Разрешаем изменение размера окна
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (s, e) => UpdateScaleMatrix();
    }

    private void UpdateScaleMatrix()
    {
        float scaleX = (float)GraphicsDevice.Viewport.Width / VirtualWidth;
        float scaleY = (float)GraphicsDevice.Viewport.Height / VirtualHeight;
        _scaleMatrix = Matrix.CreateScale(scaleX, scaleY, 1.0f);
    }

    protected override void Initialize()
    {
        PathService.EnsureInitialized();
        EnemyRegistry.GraphicsDevice = GraphicsDevice;
        
        UpdateScaleMatrix();
        
        int width = VirtualWidth;
        int height = VirtualHeight;

        _mainMenuPanel = new MainMenuPanel(width, height);
        _multiplayerMenuPanel = new MultiplayerMenuPanel(width, height);
        _lobbyDiscovery = new UdpLobbyDiscovery();
        _playerName = $"Player_{Guid.NewGuid().ToString("N").Substring(0, 4)}";
        _multiplayerMenuPanel.PlayerName = _playerName;
        _lobbyBrowserPanel = new LobbyBrowserPanel(width, height, _lobbyDiscovery);
        _lobbyPanel = new LobbyPanel(width, height, _lobbyDiscovery);
        _levelSelectionPanel = new LevelSelectionPanel(width, height);

        // Wiring Main Menu
        _mainMenuPanel.OnSingleplayerClicked += () => {
            _isSelectingLevelForLobby = false;
            _levelSelectionPanel.SetMultiplayerMode(false);
            _currentState = GameState.LevelSelection;
            _levelSelectionPanel.RefreshLevelList();
        };
        _mainMenuPanel.OnMultiplayerClicked += () => _currentState = GameState.MultiplayerMenu;

        // Wiring Multiplayer Menu
        _multiplayerMenuPanel.OnCreateLobbyClicked += () => {
            _isSelectingLevelForLobby = true;
            _levelSelectionPanel.SetMultiplayerMode(true);
            _currentState = GameState.LevelSelection;
            _levelSelectionPanel.RefreshLevelList();
        };
        _multiplayerMenuPanel.OnJoinLobbyClicked += () => {
            _currentState = GameState.LobbyBrowser;
            _lobbyBrowserPanel.RefreshLobbies();
        };
        _multiplayerMenuPanel.OnBackClicked += () => _currentState = GameState.MainMenu;

        // Wiring Lobby Browser
        _lobbyBrowserPanel.OnBackClicked += () => _currentState = GameState.MultiplayerMenu;
        _lobbyBrowserPanel.OnLobbySelected += (lobby) => {
            if (_lobbyDiscovery.JoinLobby(lobby.LobbyId, _playerName))
            {
                _currentLobbyId = lobby.LobbyId;
                _currentLobbyName = lobby.Name;
                _currentState = GameState.Lobby;
                RefreshCurrentLobbyState();
            }
            else
            {
                Console.WriteLine($"Failed to join lobby {lobby.LobbyId}");
            }
        };

        // Wiring Lobby
        _lobbyPanel.OnLeaveClicked += () => {
            _lobbyDiscovery?.LeaveLobby();
            _currentLobbyId = null;
            _currentLobbyName = string.Empty;
            _currentLobbyLevelPath = string.Empty;
            _gameStartingInLobby = false;
            _currentState = GameState.MultiplayerMenu;
        };
        _lobbyPanel.OnStartClicked += () => {
             if (!string.IsNullOrEmpty(_currentLobbyLevelPath))
             {
                 _lobbyDiscovery.SignalGameStart();
                 _gameStartingInLobby = true;
             }
        };

        _levelSelectionPanel.OnLevelSelectedExtended += (levelPath, maxPlayers) => {
            if (_isSelectingLevelForLobby)
            {
                _currentState = GameState.Lobby;
                _currentLobbyLevelPath = levelPath;
                string mapName = System.IO.Path.GetFileNameWithoutExtension(levelPath);
                string lobbyName = $"Lobby: {mapName}";
                _currentLobbyName = lobbyName;
                _currentLobbyId = _lobbyDiscovery.HostLobby(lobbyName, maxPlayers, _playerName, 0, levelPath);
                RefreshCurrentLobbyState();
            }
        };

        _levelSelectionPanel.OnLevelSelected += (levelPath) => {
            if (!_isSelectingLevelForLobby)
            {
                LoadLevel(levelPath);
            }
        };

        base.Initialize();
    }

    private void LoadLevel(string levelArchivePath)
    {
        Console.WriteLine($"Attempting to load level from archive: {levelArchivePath}");
        if (!System.IO.File.Exists(levelArchivePath))
        {
            Console.WriteLine(System.IO.Path.GetFullPath(levelArchivePath));
            Console.WriteLine($"ERROR: Level archive not found at {levelArchivePath}");
            ReturnToMenu();
            return;
        }

        try
        {
            // Сброс предыдущего состояния
            GameManager.ResetInstance();
            if (TowerController.GetInstance(this) != null) TowerController.ResetInstance();
            if (EnemyController.GetInstance(this, null) != null) EnemyController.ResetInstance();
            if (DamageDealerController.GetInstance(this) != null) DamageDealerController.ResetInstance();
            if (WaveController.GetInstance(null, null) != null) WaveController.ResetInstance();

            Console.WriteLine($"Loading level from: {levelArchivePath}");
            var loadedLevel = LevelLoader.LoadFromArchive(levelArchivePath);
            
            gameMap = loadedLevel.Map;
            gameMap.PathTexture = _pathTexture; // Устанавливаем текстуру дорожки
            
            // Инициализируем контроллеры
            damageDealerController = DamageDealerController.GetInstance(this);
            damageDealerController.DefaultTexture = _bulletTexture; // Устанавливаем текстуру пуль по умолчанию
            
            towerController = TowerController.GetInstance(this);
            towerController.DefaultTexture = _towerTexture; // Устанавливаем текстуру башен по умолчанию
            
            enemyController = EnemyController.GetInstance(this, gameMap);
            waveController = WaveController.GetInstance(enemyController, gameMap);

            
            // Загружаем волны из уровня
            foreach (var waveData in loadedLevel.Waves)
            {
                var wave = ConvertWaveDataToWave(waveData, gameMap);
                waveController.AddWave(wave);
            }
            Console.WriteLine($"Loaded {loadedLevel.Waves.Count} waves");

            // Загружаем жизни и деньги из уровня
            int startingMoney = 100;
            int startingLives = 20;

            if (loadedLevel.MoneyHealthSettings != null)
            {
                startingMoney = loadedLevel.MoneyHealthSettings.StartingMoney;
                startingLives = loadedLevel.MoneyHealthSettings.StartingLives;
            }

            Console.WriteLine($"Starting Money: {startingMoney}");
            Console.WriteLine($"Starting Lives: {startingLives}");

            // Инициализируем GameManager
            gameManager = GameManager.getInstance(
                _graphics.PreferredBackBufferWidth, 
                _graphics.PreferredBackBufferHeight, 
                gameMap, 
                startingMoney,
                startingLives,
                towerController,
                loadedLevel.TowerNames,
                loadedLevel.TowerDefinitions,
                waveController, 
                enemyController,
                damageDealerController
            );

            // Provide local player's instance id to UI so towers are owned correctly
            if (_lobbyDiscovery != null && gameManager?.UIManager != null)
            {
                gameManager.UIManager.LocalPlayerInstanceId = _lobbyDiscovery.InstanceId;
                gameManager.UIManager.ResolvePlayerName = id => GetPlayerNameById(id);

                if (!string.IsNullOrEmpty(_currentLobbyId))
                {
                    bool isHost = _lobbyDiscovery.IsHost;
                    gameManager.UIManager.StartWaveButton.IsEnabled = isHost;
                    gameManager.UIManager.OnStartWaveRequested += () =>
                    {
                        if (isHost)
                            _lobbyDiscovery.SignalWaveStart(gameManager.WaveController.CurrentWaveIndex);
                    };
                    _lastObservedHostWaveStartIndex = _lobbyDiscovery.GetLobbyWaveStartIndex(_currentLobbyId);

                    // ---- P2P game sync setup ----
                    SetupGameSync(isHost);
                }
            }
            
            // Передаем стандартные текстуры
            gameManager.DefaultTowerTexture = _towerTexture;
            gameManager.DefaultEnemyTexture = _enemyTexture;
            gameManager.DefaultBulletTexture = _pixel;

            gameManager.Defeat += () => ReturnToMenu();
            gameManager.Win += () => ReturnToMenu();
            gameManager.Disconnected += () => ReturnToMenu();
            
            _currentState = GameState.Playing;
            _showInstructions = true;
            Console.WriteLine("Level loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR loading level: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            ReturnToMenu();
        }
    }

    private void RefreshCurrentLobbyState()
    {
        if (string.IsNullOrEmpty(_currentLobbyId)) return;

        var players = _lobbyDiscovery.GetLobbyPlayers(_currentLobbyId);
        if (players.Count == 0)
        {
            // Если лобби полностью пропало, возвращаемся в меню
            _lobbyDiscovery.LeaveLobby();
            _currentLobbyId = null;
            _currentLobbyName = string.Empty;
            _currentLobbyLevelPath = string.Empty;
            _gameStartingInLobby = false;
            _currentState = GameState.MultiplayerMenu;
            return;
        }

        // Try to read the host's selected level (so clients know which level to load)
        var hostLevel = _lobbyDiscovery.GetLobbyLevelPath(_currentLobbyId);
        if (!string.IsNullOrEmpty(hostLevel))
        {
            _currentLobbyLevelPath = hostLevel;
        }

        _lobbyPanel.LoadLobby(_currentLobbyName, players[0].MaxPlayers, players, _lobbyDiscovery.InstanceId);
    }

    private void SetupGameSync(bool isHost)
    {
        _gameSyncManager?.Dispose();
        _gameSyncManager = new GameSyncManager(isHost, gameManager);

        if (!string.IsNullOrEmpty(_currentLobbyId) && _lobbyDiscovery != null)
        {
            var lobbyPlayers = _lobbyDiscovery.GetLobbyPlayers(_currentLobbyId);
            foreach (var p in lobbyPlayers)
            {
                _gameSyncManager.RegisterPlayerId(p.InstanceId);
            }
        }

        if (isHost)
        {
            _gameSyncManager.StartAsHost();
            gameManager.SetNetworkMode(isClient: false, _gameSyncManager);
        }
        else
        {
            string hostIp = _lobbyDiscovery.GetLobbyHostIp(_currentLobbyId);
            if (string.IsNullOrEmpty(hostIp))
            {
                Console.WriteLine("[GameSync] Could not determine host IP — sync disabled.");
                _gameSyncManager.Dispose();
                _gameSyncManager = null;
                return;
            }

            Console.WriteLine($"[GameSync] Client connecting to host at {hostIp}");
            _gameSyncManager.StartAsClient(hostIp);
            gameManager.SetNetworkMode(isClient: true, _gameSyncManager);
        }
    }

    private void ReturnToMenu()
    {
        _gameSyncManager?.Dispose();
        _gameSyncManager = null;
        _currentState = GameState.LevelSelection;
        _levelSelectionPanel.RefreshLevelList();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Создаём пиксельную текстуру для отрисовки линий/прямоугольников
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
        
        // Загружаем шрифт
        try
        {
            _font = Content.Load<SpriteFont>("EngineFont");
            Console.WriteLine("Font loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load font: {ex.Message}");
            _font = null;
        }

        _levelSelectionPanel.LoadContent(_font, GraphicsDevice);

        // Пытаемся загрузить картинку башни из файла
        string towerImagePath = PathService.GetCommonFilePath("tower.png");
        if (File.Exists(towerImagePath))
        {
            try
            {
                using (var stream = File.OpenRead(towerImagePath))
                {
                    _towerTexture = Texture2D.FromStream(GraphicsDevice, stream);
                    Console.WriteLine($"Tower sprite loaded from: {towerImagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tower image: {ex.Message}. Falling back to procedural.");
                _towerTexture = CreateProceduralTowerTexture();
            }
        }
        else
        {
            _towerTexture = CreateProceduralTowerTexture();
        }

        // Пытаемся загрузить картинку пули из файла
        string bulletImagePath = PathService.GetCommonFilePath("bullet.png");
        if (File.Exists(bulletImagePath))
        {
            try
            {
                using (var stream = File.OpenRead(bulletImagePath))
                {
                    _bulletTexture = Texture2D.FromStream(GraphicsDevice, stream);
                    Console.WriteLine($"Bullet sprite loaded from: {bulletImagePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bullet image: {ex.Message}. Falling back to procedural.");
                _bulletTexture = CreateProceduralBulletTexture();
            }
        }
        else
        {
            _bulletTexture = CreateProceduralBulletTexture();
        }

        // Создаём временную текстуру для врага (зелёный квадрат 30x30)
        _enemyTexture = new Texture2D(GraphicsDevice, 30, 30);
        Color[] enemyData = new Color[30 * 30];
        for (int i = 0; i < enemyData.Length; i++) enemyData[i] = Color.Green;
        _enemyTexture.SetData(enemyData);
        
        EnemyRegistry.DefaultTexture = _enemyTexture;

        // Создаём процедурную текстуру камня для дорожки (64x64)
        _pathTexture = new Texture2D(GraphicsDevice, 64, 64);
        Color[] pathData = new Color[64 * 64];
        Random rand = new Random();
        for (int i = 0; i < pathData.Length; i++)
        {
            int x = i % 64;
            int y = i / 64;
            
            // Базовый серый цвет с шумом
            int gray = rand.Next(100, 150);
            
            // Добавляем "текстурность" (небольшие пятна)
            if (rand.Next(100) > 95) gray -= 30;
            if (rand.Next(100) > 95) gray += 30;
            
            // Темная рамка для эффекта плитки/камней
            if (x < 2 || x > 61 || y < 2 || y > 61) gray -= 40;
            
            gray = Math.Clamp(gray, 0, 255);
            pathData[i] = new Color(gray, gray, gray);
        }
        _pathTexture.SetData(pathData);
    }

    private Texture2D CreateProceduralTowerTexture()
    {
        Texture2D texture = new Texture2D(GraphicsDevice, 80, 80);
        Color[] towerData = new Color[80 * 80];
        for (int y = 0; y < 80; y++)
        {
            for (int x = 0; x < 80; x++)
            {
                int i = y * 80 + x;
                Color color = Color.Transparent;

                // Основание (камень) - нижние 20 пикселей
                if (y >= 60)
                {
                    color = (x % 20 < 4 || y % 10 == 0) ? Color.DarkGray : Color.Gray;
                }
                // Основная часть (дерево/камень) - от 16 до 60
                else if (y >= 16 && x >= 10 && x <= 69)
                {
                    color = (x == 10 || x == 69) ? Color.Black : Color.SaddleBrown;
                    // Окно - по центру
                    if (y >= 30 && y <= 40 && x >= 36 && x <= 43) color = Color.Yellow;
                }
                // Зубцы башни (верх) - верхние 16 пикселей
                else if (y < 16 && x >= 6 && x <= 73)
                {
                    bool isTooth = (x % 20 < 12);
                    if (isTooth || y >= 10) color = Color.DimGray;
                }
                towerData[i] = color;
            }
        }
        texture.SetData(towerData);
        return texture;
    }

    private Texture2D CreateProceduralBulletTexture()
    {
        int size = 32;
        Texture2D texture = new Texture2D(GraphicsDevice, size, size);
        Color[] data = new Color[size * size];
        
        float centerX = size / 2f;
        float centerY = size / 2f;
        float radius = size / 2f - 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (dist <= radius)
                {
                    // Базовый цвет (темно-серый металл)
                    int shade = 60 + (int)(dist / radius * 40);
                    Color color = new Color(shade, shade, shade);
                    
                    // Добавляем блик (смещение от центра)
                    float lx = x - (centerX - 5);
                    float ly = y - (centerY - 5);
                    float lDist = (float)Math.Sqrt(lx * lx + ly * ly);
                    if (lDist < 8)
                    {
                        float intensity = 1.0f - (lDist / 8f);
                        color = Color.Lerp(color, Color.White, intensity * 0.8f);
                    }
                    
                    data[y * size + x] = color;
                }
                else
                {
                    data[y * size + x] = Color.Transparent;
                }
            }
        }
        texture.SetData(data);
        return texture;
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState ks = Keyboard.GetState();
        MouseState ms = Mouse.GetState();

        // Трансформируем координаты мыши в виртуальное пространство (1600x900)
        float scaleX = (float)VirtualWidth / GraphicsDevice.Viewport.Width;
        float scaleY = (float)VirtualHeight / GraphicsDevice.Viewport.Height;
        MouseState virtualMs = new MouseState(
            (int)(ms.X * scaleX),
            (int)(ms.Y * scaleY),
            ms.ScrollWheelValue,
            ms.LeftButton,
            ms.MiddleButton,
            ms.RightButton,
            ms.XButton1,
            ms.XButton2
        );
        
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (IsActive && ks.IsKeyDown(Keys.Escape)))
            Exit();

        switch (_currentState)
        {
            case GameState.MainMenu:
                _mainMenuPanel.Update(gameTime, virtualMs, _previousMouseState);
                break;
            case GameState.MultiplayerMenu:
                _multiplayerMenuPanel.Update(gameTime, virtualMs, _previousMouseState, ks, _previousKeyboardState);
                _playerName = _multiplayerMenuPanel.PlayerName;
                break;
            case GameState.LobbyBrowser:
                _lobbyBrowserPanel.Update(gameTime, virtualMs, _previousMouseState);
                break;
            case GameState.Lobby:
                _lobbyDiscovery.KeepAlive();
                if (DateTimeOffset.UtcNow - _lastLobbyStateRefresh >= TimeSpan.FromSeconds(1))
                {
                    _lastLobbyStateRefresh = DateTimeOffset.UtcNow;
                    RefreshCurrentLobbyState();
                }
                _lobbyPanel.Update(gameTime, virtualMs, _previousMouseState);
                
                // Check if host signaled game start; clients may not have _gameStartingInLobby set
                if (!string.IsNullOrEmpty(_currentLobbyId))
                {
                    if (_lobbyDiscovery.IsLobbyGameStarting(_currentLobbyId))
                    {
                        if (string.IsNullOrEmpty(_currentLobbyLevelPath))
                        {
                            _currentLobbyLevelPath = _lobbyDiscovery.GetLobbyLevelPath(_currentLobbyId) ?? string.Empty;
                        }

                        if (!string.IsNullOrEmpty(_currentLobbyLevelPath))
                        {
                            LoadLevel(_currentLobbyLevelPath);
                        }
                    }
                }
                break;
            case GameState.LevelSelection:
                if (IsActive)
                    _levelSelectionPanel.Update(ks, _previousKeyboardState);
                break;
            case GameState.Playing:
                if (_showInstructions)
                {
                    if (IsActive &&
                        ((ks.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
                         (ks.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space))))
                    {
                        _showInstructions = false;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(_currentLobbyId))
                    {
                        _lobbyDiscovery.KeepAlive();
                        int hostWaveStartIndex = _lobbyDiscovery.GetLobbyWaveStartIndex(_currentLobbyId);
                        if (hostWaveStartIndex > _lastObservedHostWaveStartIndex)
                        {
                            _lastObservedHostWaveStartIndex = hostWaveStartIndex;
                            if (gameManager?.WaveController != null &&
                                !gameManager.WaveController.IsWaveActive &&
                                gameManager.WaveController.CurrentWaveIndex == hostWaveStartIndex)
                            {
                                Console.WriteLine($"Host requested wave start for wave {hostWaveStartIndex}");
                                gameManager.StartWave();
                            }
                        }
                    }

                    gameManager?.Update(gameTime);
                    
                    bool canStartWaveLocally = string.IsNullOrEmpty(_currentLobbyId) || _lobbyDiscovery.IsHost;
                    if (IsActive && canStartWaveLocally && ks.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter))
                    {
                        if (!string.IsNullOrEmpty(_currentLobbyId) && _lobbyDiscovery.IsHost)
                        {
                            _lobbyDiscovery.SignalWaveStart(gameManager.WaveController.CurrentWaveIndex);
                        }

                        gameManager?.StartWave();
                    }

                    if (IsActive && ks.IsKeyDown(Keys.Back) && _previousKeyboardState.IsKeyUp(Keys.Back))
                    {
                        ReturnToMenu();
                    }
                }
                break;
        }

        _previousKeyboardState = ks;
        _previousMouseState = virtualMs;
        base.Update(gameTime);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _gameSyncManager?.Dispose();
        if (_lobbyDiscovery is IDisposable d)
            d.Dispose();
        base.OnExiting(sender, args);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Используем SamplerState.PointClamp чтобы спрайты были четкими и не "двоились" при анимации
        _spriteBatch.Begin(
            SpriteSortMode.Deferred, 
            BlendState.AlphaBlend, 
            SamplerState.PointClamp, 
            null, 
            null, 
            null, 
            _scaleMatrix
        );

        switch (_currentState)
        {
            case GameState.MainMenu:
                _mainMenuPanel.Draw(_spriteBatch, _pixel, _font);
                break;
            case GameState.MultiplayerMenu:
                _multiplayerMenuPanel.Draw(_spriteBatch, _pixel, _font);
                break;
            case GameState.LobbyBrowser:
                _lobbyBrowserPanel.Draw(_spriteBatch, _pixel, _font);
                break;
            case GameState.Lobby:
                _lobbyPanel.Draw(_spriteBatch, _pixel, _font);
                break;
            case GameState.LevelSelection:
                _levelSelectionPanel.Draw(_spriteBatch);
                break;
            case GameState.Playing:
                if (gameManager != null)
                {
                    gameManager.Draw(_spriteBatch, _pixel, _font);
                    if (_showInstructions)
                    {
                        DrawInstructions();
                    }
                }
                break;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawInstructions()
    {
        if (_font == null) return;

        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;
        
        // Полупрозрачный фон
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.7f);
        
        // Заголовок
        string title = "=== УПРАВЛЕНИЕ ИГРОЙ ===";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, 100);
        _spriteBatch.DrawString(_font, title, titlePos, Color.Yellow);
        
        // Инструкции
        string[] instructions = new string[]
        {
            "",
            "ESC - Выход из игры",
            "",
            "Левая кнопка мыши - Взаимодействие с UI",
            "",
            "ENTER или ПРОБЕЛ - Закрыть это окно",
            "",
            "",
            "Нажмите ENTER или ПРОБЕЛ чтобы начать игру..."
        };
        
        float yOffset = 200;
        foreach (var line in instructions)
        {
            Vector2 lineSize = _font.MeasureString(line);
            Vector2 linePos = new Vector2((screenWidth - lineSize.X) / 2, yOffset);
            Color color = line.Contains("Нажмите") ? Color.Lime : Color.White;
            _spriteBatch.DrawString(_font, line, linePos, color);
            yOffset += 40;
        }
    }

    /// <summary>
    /// Конвертирует WaveData из архива в Wave для SimulationEngine
    /// </summary>
    private Wave ConvertWaveDataToWave(LevelLoader.WaveData waveData, GameMap gameMap)
    {
        var wave = new Wave($"wave_{waveData.Index}");
        
        foreach (var spawn in waveData.Spawns)
        {
            // Находим точку спавна по ID
            var spawnPoint = gameMap.SpawnPoints.FirstOrDefault(sp => sp.Id == spawn.SpawnPointId);
            if (spawnPoint == null)
            {
                Console.WriteLine($"Warning: Spawn point {spawn.SpawnPointId} not found for enemy {spawn.EnemyTypeId}");
                continue;
            }

            // Получаем определение врага
            var enemyDef = EnemyRegistry.create(spawn.EnemyTypeId);
            if (enemyDef == null)
            {
                Console.WriteLine($"Warning: Enemy definition {spawn.EnemyTypeId} not found");
                continue;
            }

            // Используем BasicEnemyType как placeholder тип, но сохраняем строковый ID
            // WaveController будет использовать строковый ID для создания правильных врагов через фабрику
            wave.AddEnemy(spawn.EnemyTypeId, spawn.Count, spawnPoint, spawn.EnemyTypeId);
            Console.WriteLine($"Added to wave {waveData.Index}: {spawn.Count}x {spawn.EnemyTypeId} at {spawn.SpawnPointId}");
        }
        
        return wave;
    }

    private string GetPlayerNameById(string id)
    {
        if (string.IsNullOrEmpty(_currentLobbyId) || _lobbyDiscovery == null)
        {
            return _playerName;
        }
        var players = _lobbyDiscovery.GetLobbyPlayers(_currentLobbyId);
        var player = players.FirstOrDefault(p => p.InstanceId == id);
        return player != null ? player.PlayerName : $"Player_{id.Substring(0, Math.Min(4, id.Length))}";
    }
}
