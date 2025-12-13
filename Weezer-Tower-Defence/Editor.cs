using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine;

namespace Weezer_Tower_Defence;

public class Editor : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private LevelEditor levelEditor;
    private Texture2D pixel; // Single pixel texture for drawing lines/shapes

    public Editor()
    {
        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        graphics.PreferredBackBufferWidth = 1920;
        graphics.PreferredBackBufferHeight = 1080;
    }

    protected override void Initialize()
    {
        base.Initialize();
        levelEditor = new LevelEditor(Content);
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        
        // Create a simple 1x1 white pixel texture for drawing
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || 
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();

        levelEditor.Update(gameTime, keyboardState, mouseState);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black * 0.8f);

        spriteBatch.Begin();
        levelEditor.Draw(spriteBatch, pixel);
        spriteBatch.End();

        base.Draw(gameTime);
    }
}