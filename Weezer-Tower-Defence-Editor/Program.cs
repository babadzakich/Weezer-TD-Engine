﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine;
using System;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Weezer Tower Defence...");
            Register.setup();
            using var game = new Editor();
            game.Run();
        }
    }

    public class EditorGame : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private LevelEditor _editor;
        private Texture2D _pixel;
        private SpriteFont _font;
        private bool _showInstructions = true;
        private KeyboardState _previousKeyState;

        public EditorGame()
        {
            _graphics = new GraphicsDeviceManager(this);

            // Полноэкранный режим без рамок
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            Window.IsBorderless = false;

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            
            // Разрешаем изменение размера окна
            Window.AllowUserResizing = true;

        }


        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Загружаем шрифт
            try
            {
                _font = Content.Load<SpriteFont>("DefaultFont");
            }
            catch
            {
                _font = null;
            }

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            int screenWidth = GraphicsDevice.Viewport.Width;
            int screenHeight = GraphicsDevice.Viewport.Height;
            _editor = new LevelEditor(Content, screenWidth, screenHeight);
        }

        protected override void Update(GameTime gameTime)
        {
            var currentKeyState = Keyboard.GetState();
            
            if (currentKeyState.IsKeyDown(Keys.Escape))
                Exit();

            // Закрыть инструкции по нажатию Enter или пробела
            if (_showInstructions && 
                (currentKeyState.IsKeyDown(Keys.Enter) && _previousKeyState.IsKeyUp(Keys.Enter) ||
                 currentKeyState.IsKeyDown(Keys.Space) && _previousKeyState.IsKeyUp(Keys.Space)))
            {
                _showInstructions = false;
            }

            if (!_showInstructions)
            {
                _editor.Update(gameTime, currentKeyState, Mouse.GetState());
            }

            _previousKeyState = currentKeyState;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            _editor.Draw(_spriteBatch, _pixel);
            
            // Рисуем инструкции поверх всего
            if (_showInstructions && _font != null)
            {
                DrawInstructions();
            }
            
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
            string title = "=== УПРАВЛЕНИЕ РЕДАКТОРОМ ===";
            Vector2 titleSize = _font.MeasureString(title);
            Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, 80);
            _spriteBatch.DrawString(_font, title, titlePos, Color.Yellow);
            
            // Инструкции
            string[] instructions = new string[]
            {
                "",
                "ESC - Выход из редактора",
                "",
                "Левая кнопка мыши - Выбор и перемещение объектов",
                "Правая кнопка мыши - Удаление объектов",
                "",
                "Панель слева - Выбор типа объекта для размещения",
                "Панель справа - Редактирование свойств выбранного объекта",
                "",
                "S - Сохранить карту",
                "L - Загрузить карту",
                "",
                "",
                "ENTER или ПРОБЕЛ - Закрыть это окно",
                "",
                "Нажмите ENTER или ПРОБЕЛ чтобы начать редактирование..."
            };
            
            float yOffset = 150;
            foreach (var line in instructions)
            {
                Vector2 lineSize = _font.MeasureString(line);
                Vector2 linePos = new Vector2((screenWidth - lineSize.X) / 2, yOffset);
                Color color = line.Contains("Нажмите") ? Color.Lime : Color.White;
                _spriteBatch.DrawString(_font, line, linePos, color);
                yOffset += 35;
            }
        }
    }
}
