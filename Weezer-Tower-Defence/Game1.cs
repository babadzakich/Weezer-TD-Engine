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

namespace Weezer_Tower_Defence;

public class Game1 : Game
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

    private enum GameState
    {
        LevelSelection,
        Playing
    }
    private GameState _currentState = GameState.LevelSelection;
    private LevelSelectionPanel _levelSelectionPanel;
    private KeyboardState _previousKeyboardState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1600;
        _graphics.PreferredBackBufferHeight = 900;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        
        // Разрешаем изменение размера окна
        Window.AllowUserResizing = true;
        _graphics.PreferredBackBufferWidth = 1600;
        _graphics.PreferredBackBufferHeight = 900;
    }

    protected override void Initialize()
    {
        _levelSelectionPanel = new LevelSelectionPanel();
        _levelSelectionPanel.OnLevelSelected += LoadLevel;
        
        base.Initialize();
    }

    private void LoadLevel(string levelArchivePath)
    {
        Console.WriteLine($"Attempting to load level from archive: {levelArchivePath}");
        if (!System.IO.File.Exists(levelArchivePath))
        {
            Console.WriteLine($"ERROR: Level archive not found at {levelArchivePath}");
            return;
        }

        // Сброс старого состояния перед загрузкой нового уровня
        ResetGameState();

        try
        {
            Console.WriteLine($"Loading level from: {levelArchivePath}");
            var loadedLevel = LevelLoader.LoadFromArchive(levelArchivePath);
            
            gameMap = loadedLevel.Map;
            Console.WriteLine($"Loaded map: {gameMap.Name} ({gameMap.Width}x{gameMap.Height})");
            
            // Загружаем определения врагов в фабрику
            EnemyTypeFactory.Instance.LoadEnemyTypesFromLevel(loadedLevel.EnemyDefinitions);
            
            // Инициализируем контроллеры
            damageDealerController = DamageDealerController.GetInstance(this);
            towerController = TowerController.GetInstance(this);
            enemyController = EnemyController.GetInstance(this, gameMap);
            waveController = WaveController.GetInstance(enemyController, gameMap);
            
            // Загружаем волны из уровня
            foreach (var waveData in loadedLevel.Waves)
            {
                var wave = ConvertWaveDataToWave(waveData, gameMap);
                waveController.AddWave(wave);
            }
            
            // Инициализируем GameManager
            gameManager = GameManager.getInstance(
                _graphics.PreferredBackBufferWidth, 
                _graphics.PreferredBackBufferHeight, 
                gameMap, 
                towerController, 
                waveController, 
                enemyController,
                damageDealerController,
                loadedLevel.TowerDefinitions
            );
            gameManager.DefaultTowerTexture = _towerTexture;
            gameManager.DefaultBulletTexture = _bulletTexture;
            gameManager.Defeat += ReturnToMenu;
            gameManager.Win += ReturnToMenu;
            
            // Инициализация графических ресурсов, которые зависят от загруженного уровня
            if (_enemyTexture != null)
                waveController.SetEnemyTexture(_enemyTexture);
            
            // Также при загрузке уровня нужно убедиться, что пули (если они есть) получают текстуру
            // Хотя при старте уровня их обычно нет, но для надежности:
            if (_bulletTexture != null)
            {
                foreach (var bullet in damageDealerController.damageDealers)
                {
                    bullet.Texture = _bulletTexture;
                }
            }
                
            // Запускаем первую волну
            waveController.StartNextWave();
            
            _currentState = GameState.Playing;
            Console.WriteLine("Level loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR loading level: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    private void ResetGameState()
    {
        TowerController.ResetInstance();
        EnemyController.ResetInstance();
        DamageDealerController.ResetInstance();
        WaveController.ResetInstance();
        GameManager.ResetInstance();
        
        towerController = null;
        enemyController = null;
        damageDealerController = null;
        waveController = null;
        gameManager = null;
        gameMap = null;
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
        _pixel = new Texture2D(GraphicsDevice, 10, 10);
        Color[] pixeldata = new Color[10 * 10];
        for (int i = 0; i < pixeldata.Length; i++) pixeldata[i] = Color.White;
        _pixel.SetData(pixeldata);
        
        // Загружаем шрифт
        try
        {
            _font = Content.Load<SpriteFont>("DefaultFont");
            Console.WriteLine("Font loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Could not load font: {ex.Message}");
            _font = null;
        }

        // Создаём временную текстуру для пули (красный квадрат 10x10)
        _bulletTexture = new Texture2D(GraphicsDevice, 100, 100);
        Color[] bulletData = new Color[100 * 100];
        for (int i = 0; i < bulletData.Length; i++) bulletData[i] = Color.Red;
        _bulletTexture.SetData(bulletData);

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

        // Присваиваем текстуру всем пулям в контроллере
        if (damageDealerController != null)
        {
            foreach (var bullet in damageDealerController.damageDealers)
            {
                bullet.Texture = _bulletTexture;
            }
        }
        
        // Присваиваем текстуру WaveController для врагов
        if (waveController != null)
            waveController.SetEnemyTexture(_enemyTexture);
    }

    protected override void Update(GameTime gameTime)
    {
        var currentKeyState = Keyboard.GetState();
        
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || currentKeyState.IsKeyDown(Keys.Escape))
            Exit();

        if (_currentState == GameState.LevelSelection)
        {
            _levelSelectionPanel.Update(currentKeyState, _previousKeyboardState);
            _previousKeyboardState = currentKeyState;
            return;
        }

        if (currentKeyState.IsKeyDown(Keys.Back) && _previousKeyboardState.IsKeyUp(Keys.Back))
        {
            ReturnToMenu();
            _previousKeyboardState = currentKeyState;
            return;
        }

        gameManager.Update(gameTime);
        
        // Обновляем UI информацию
        gameManager.UIManager.Wave = waveController.CurrentWaveIndex + 1;
        
        // Проверяем поражение
        if (gameMap.DefensePoints.Count > 0 && gameMap.DefensePoints[0].IsDestroyed)
        {
            // Game Over
            ReturnToMenu();
        }

        base.Update(gameTime);
        _previousKeyboardState = currentKeyState;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        if (_currentState == GameState.LevelSelection)
        {
            _spriteBatch.Begin();
            _levelSelectionPanel.Draw(_spriteBatch, _font ?? Content.Load<SpriteFont>("DefaultFont"), _pixel, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _spriteBatch.End();
            return;
        }

        _spriteBatch.Begin();
        gameMap.Draw(_spriteBatch, _pixel);
        towerController.Draw(_spriteBatch);
        damageDealerController.Draw(_spriteBatch);
        enemyController.Draw(_spriteBatch);
        waveController.Draw(_spriteBatch);
        gameManager.UIManager.Draw(_spriteBatch, _pixel, _font);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawInstructions()
    {
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
            var enemyDef = EnemyTypeFactory.Instance.GetEnemyDefinition(spawn.EnemyTypeId);
            if (enemyDef == null)
            {
                Console.WriteLine($"Warning: Enemy definition {spawn.EnemyTypeId} not found");
                continue;
            }

            // Используем BasicEnemyType как placeholder тип, но сохраняем строковый ID
            // WaveController будет использовать строковый ID для создания правильных врагов через фабрику
            wave.AddEnemy(typeof(BasicEnemyType), spawn.Count, spawnPoint, spawn.EnemyTypeId);
            Console.WriteLine($"Added to wave {waveData.Index}: {spawn.Count}x {spawn.EnemyTypeId} at {spawn.SpawnPointId}");
        }
        
        return wave;
    }
}
