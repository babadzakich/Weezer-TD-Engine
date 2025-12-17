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
        // Загружаем уровень из архива
        string levelArchivePath = "Content/level_1_package.zip";
        
        if (!System.IO.File.Exists(levelArchivePath))
        {
            Console.WriteLine($"ERROR: Level archive not found at {levelArchivePath}");
            Console.WriteLine("Please create a level in the editor (dotnet run -- editor) and press Ctrl+P to package it.");
            Exit();
            return;
        }

        try
        {
            Console.WriteLine($"Loading level from: {levelArchivePath}");
            var loadedLevel = LevelLoader.LoadFromArchive(levelArchivePath);
            
            gameMap = loadedLevel.Map;
            Console.WriteLine($"Loaded map: {gameMap.Name} ({gameMap.Width}x{gameMap.Height})");
            Console.WriteLine($"- Spawn points: {gameMap.SpawnPoints.Count}");
            Console.WriteLine($"- Defense points: {gameMap.DefensePoints.Count}");
            Console.WriteLine($"- Paths: {gameMap.Paths.Count}");
            Console.WriteLine($"- Build zones: {gameMap.BuildZones.Count}");
            
            // Загружаем определения врагов в фабрику
            EnemyTypeFactory.Instance.LoadEnemyTypesFromLevel(loadedLevel.EnemyDefinitions);
            Console.WriteLine($"Loaded {loadedLevel.EnemyDefinitions.Count} enemy definitions into factory");
            
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
            Console.WriteLine($"Loaded {loadedLevel.Waves.Count} waves");
            
            // Инициализируем GameManager
            gameManager = GameManager.getInstance(
                _graphics.PreferredBackBufferWidth, 
                _graphics.PreferredBackBufferHeight, 
                gameMap, 
                towerController, 
                waveController, 
                enemyController
            );
            gameManager.OnGameOver += () => Exit();
            
            Console.WriteLine("Level loaded successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR loading level: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Exit();
            return;
        }

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Создаём пиксельную текстуру для отрисовки линий/прямоугольников
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
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
        _bulletTexture = new Texture2D(GraphicsDevice, 10, 10);
        Color[] bulletData = new Color[10 * 10];
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
        foreach (var bullet in damageDealerController.damageDealers)
        {
            bullet.Texture = _bulletTexture;
        }
        
        // Присваиваем текстуру WaveController для врагов
        waveController.SetEnemyTexture(_enemyTexture);
        
        // Запускаем первую волну
        waveController.StartNextWave();
    }

    protected override void Update(GameTime gameTime)
    {
        var currentKeyState = Keyboard.GetState();
        
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || currentKeyState.IsKeyDown(Keys.Escape))
            Exit();

        damageDealerController.Update(gameTime);
        towerController.Update(gameTime);
        enemyController.Update(gameTime);
        waveController.Update(gameTime);
        gameManager.Update(gameTime);
        
        // Обновляем UI информацию
        gameManager.UIManager.Wave = waveController.CurrentWaveIndex + 1;
        
        // Проверяем поражение
        if (gameMap.DefensePoints.Count > 0 && gameMap.DefensePoints[0].IsDestroyed)
        {
            // Game Over
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

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
