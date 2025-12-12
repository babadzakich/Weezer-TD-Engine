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
    }

    protected override void Initialize()
    {
        // Создаём тестовую карту
        gameMap = new GameMap("test_map", "Test Map", 1600, 900);
        
        // Создаём путь со сплайном
        var path = new Path("path1", "defense1", useSmoothPath: true, splineResolution: 25);
        path.AddWaypoints(
            new Vector2(50, 300),
            new Vector2(200, 100),
            new Vector2(400, 300),
            new Vector2(600, 150),
            new Vector2(750, 300)
        );
        gameMap.AddPath(path);
        
        // Точка защиты
        gameMap.AddDefensePoint(new DefensePoint(new Vector2(750, 300), "defense1", 100));
        
        // Точка спавна
        gameMap.AddSpawnPoint(new SpawnPoint(new Vector2(50, 300), "spawn1", "path1"));
        
        // Зоны строительства
        gameMap.AddBuildZone(new BuildZone(new Vector2(200, 250), "zone1", new Vector2(50, 50)));
        gameMap.AddBuildZone(new BuildZone(new Vector2(400, 450), "zone2", new Vector2(50, 50)));
        gameMap.AddBuildZone(new BuildZone(new Vector2(600, 250), "zone3", new Vector2(50, 50)));

        damageDealerController = DamageDealerController.GetInstance(this);
        damageDealerController.AddDamageDealer(
            new DamageDealer(
                new StandardBulletBehavior(20f, 300f, 500f, null),
                new Vector2(100, 100),
                new Vector2(1, 1)
            )
        );

        towerController = TowerController.GetInstance(this);
        towerController.AddTower(
            new Tower(
                new BasicTowerBehavior("basic_tower", "Basic Tower", new StandardBulletBehavior(25f, 300f, 500f, null), 100, 150f, 1f),
                new Vector2(400, 300) // Позиция башни в центре экрана
            )
        );

        enemyController = EnemyController.GetInstance(this, gameMap);
        
        // Инициализируем WaveController
        waveController = WaveController.GetInstance(enemyController, gameMap);
        
        // Создаём тестовые волны
        var wave1 = new Wave("wave1");
        wave1.AddEnemy(typeof(BasicEnemyType), 5, gameMap.SpawnPoints[0]);
        waveController.AddWave(wave1);
        
        var wave2 = new Wave("wave2");
        wave2.AddEnemy(typeof(BasicEnemyType), 8, gameMap.SpawnPoints[0]);
        waveController.AddWave(wave2);
        
        var wave3 = new Wave("wave3");
        wave3.AddEnemy(typeof(BasicEnemyType), 10, gameMap.SpawnPoints[0]);
        waveController.AddWave(wave3);
        
        // Инициализируем GameManager с UI
        gameManager = GameManager.getInstance(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, gameMap, towerController, waveController, enemyController);
        gameManager.OnGameOver += () => Exit();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Создаём пиксельную текстуру для отрисовки линий/прямоугольников
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        
        // Загружаем шрифт (если есть), иначе используем null
        try { _font = Content.Load<SpriteFont>("Fonts/Default"); } 
        catch { _font = null; }

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
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
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
}
