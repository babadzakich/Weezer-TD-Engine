using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.DamageDealers;
using SimulationEngine.TowerRelated;
using SimulationEngine.MapRelated;

namespace Weezer_Tower_Defence;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _bulletTexture;
    private Texture2D _towerTexture;
    private Texture2D _pixel;

    private DamageDealerController damageDealerController;
    private TowerController towerController;
    private GameMap gameMap;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
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
                ProjectileConfig.Default,
                new Vector2(100, 100),
                new Vector2(1, 1)
            )
        );

        towerController = TowerController.GetInstance(this);
        towerController.AddTower(
            new Tower(
                SniperTowerConfig.Default,
                new Vector2(400, 300) // Позиция башни в центре экрана
            )
        );

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Создаём пиксельную текстуру для отрисовки линий/прямоугольников
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

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

        // Присваиваем текстуру всем пулям в контроллере
        foreach (var bullet in damageDealerController.damageDealers)
        {
            bullet.Texture = _bulletTexture;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        damageDealerController.Update(gameTime);
        towerController.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        gameMap.Draw(_spriteBatch, _pixel);
        towerController.Draw(_spriteBatch);
        damageDealerController.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
