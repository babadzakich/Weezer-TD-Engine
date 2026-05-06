using System;
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
using System.Linq;
using SimulationEngine.Infrastructure;

namespace Weezer_Tower_Defence;

public class GameRunner : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _bulletTexture;
    private Texture2D _towerTexture;
    private Texture2D _enemyTexture;
    private Texture2D _pixel;
    private SpriteFont _font;

    private DamageDealerController damageDealerController;
    private TowerController towerController;
    private EnemyController enemyController;
    private WaveController waveController;
    private GameManager gameManager;
    private GameMap gameMap;

    private LevelSelectionPanel _levelSelectionPanel;
    private GameState _currentState = GameState.LevelSelection;
    private bool _showInstructions = false;
    private KeyboardState _previousKeyboardState;

    public enum GameState
    {
        LevelSelection,
        Playing
    }

    public GameRunner()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1600,
            PreferredBackBufferHeight = 900
        };
        var commonRoot = PathService.CommonDirectory;

        Console.WriteLine($"Common content root: {commonRoot}");
        
        Content.RootDirectory = $"{commonRoot}";
        IsMouseVisible = true;
        
        // Разрешаем изменение размера окна
        Window.AllowUserResizing = true;
    }

    protected override void Initialize()
    {
        _levelSelectionPanel = new LevelSelectionPanel(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
        _levelSelectionPanel.OnLevelSelected += LoadLevel;

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
            
            // Инициализируем контроллеры
            damageDealerController = DamageDealerController.GetInstance(this);
            towerController = TowerController.GetInstance(this);
            enemyController = EnemyController.GetInstance(this, gameMap);
            waveController = WaveController.GetInstance(enemyController, gameMap);

            // Назначаем текстуры пулям (важно сделать это после инициализации контроллера)
            if (damageDealerController != null)
            {
                foreach (var bullet in damageDealerController.DamageDealers)
                {
                    bullet.Texture = _pixel;
                }
            }
            

            
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
            
            // Передаем стандартные текстуры
            gameManager.DefaultTowerTexture = _towerTexture;
            gameManager.DefaultEnemyTexture = _enemyTexture;
            gameManager.DefaultBulletTexture = _pixel;

            gameManager.Defeat += () => ReturnToMenu();
            gameManager.Win += () => ReturnToMenu();
            
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

    private void ReturnToMenu()
    {
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

        // Создаём временную текстуру для башни (синий квадрат 40x40)
        _towerTexture = new Texture2D(GraphicsDevice, 40, 40);
        Color[] towerData = new Color[40 * 40];
        for (int i = 0; i < towerData.Length; i++) towerData[i] = Color.Blue;
        _towerTexture.SetData(towerData);

        // Создаём временную текстуру для врага (зелёный квадрат 30x30)
        _enemyTexture = new Texture2D(GraphicsDevice, 30, 30);
        Color[] enemyData = new Color[30 * 30];
        for (int i = 0; i < enemyData.Length; i++) enemyData[i] = Color.Green;
        _enemyTexture.SetData(enemyData);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState ks = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || ks.IsKeyDown(Keys.Escape))
            Exit();

        if (_currentState == GameState.LevelSelection)
        {
            _levelSelectionPanel.Update(ks, _previousKeyboardState);
        }
        else
        {
            if (_showInstructions)
            {
                if ((ks.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter)) ||
                    (ks.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space)))
                {
                    _showInstructions = false;
                }
            }
            else
            {
                gameManager?.Update(gameTime);
                
                // Запуск волны по Enter
                if (ks.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter))
                {
                    gameManager?.StartWave();
                }

                if (ks.IsKeyDown(Keys.Back) && _previousKeyboardState.IsKeyUp(Keys.Back))
                {
                    ReturnToMenu();
                }
            }
        }

        _previousKeyboardState = ks;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();

        if (_currentState == GameState.LevelSelection)
        {
            _levelSelectionPanel.Draw(_spriteBatch);
        }
        else if (gameManager != null)
        {
            gameManager.Draw(_spriteBatch, _pixel, _font);
            
            if (_showInstructions)
            {
                DrawInstructions();
            }
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
}
