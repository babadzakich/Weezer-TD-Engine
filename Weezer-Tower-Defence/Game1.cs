using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.ProjectileConfig;

namespace Weezer_Tower_Defence;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _bulletTexture;

    private DamageDealerController damageDealerController;

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

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Создаём временную текстуру для пули (белый квадрат 10x10)
        _bulletTexture = new Texture2D(GraphicsDevice, 10, 10);
        Color[] data = new Color[10 * 10];
        for (int i = 0; i < data.Length; i++) data[i] = Color.Red;
        _bulletTexture.SetData(data);

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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin();
        damageDealerController.Draw(_spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
