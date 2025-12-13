using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main()
        {
            using var game = new EditorGame();
            game.Run();
        }
    }

    public class EditorGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private LevelEditor _editor;
        private Texture2D _pixel;

        public EditorGame()
{
    _graphics = new GraphicsDeviceManager(this);

    _graphics.PreferredBackBufferWidth = 1920;
    _graphics.PreferredBackBufferHeight = 1080;

    Content.RootDirectory = "Content";
    IsMouseVisible = true;
}


        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _editor = new LevelEditor(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _editor.Update(gameTime, Keyboard.GetState(), Mouse.GetState());
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            _editor.Draw(_spriteBatch, _pixel);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
