using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.DamageDealers;
using SimulationEngine.TowerRelated;

namespace Weezer_Tower_Defence;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _bulletTexture;
    private Texture2D _towerTexture;

    private DamageDealerController damageDealerController;
    private TowerController towerController;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
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
        towerController.Draw(_spriteBatch);
        damageDealerController.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
