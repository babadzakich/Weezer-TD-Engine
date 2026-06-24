using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine;
using System;
using System.IO;
using SimulationEngine.Infrastructure;

namespace Weezer_Tower_Defence;

public class Editor : Game
{
    private GraphicsDeviceManager graphics;
    private SpriteBatch spriteBatch;
    private LevelEditor levelEditor;
    private Texture2D pixel; // Single pixel texture for drawing lines/shapes
    private SpriteFont font;
    private bool showInstructions = true;
    private KeyboardState previousKeyState;

    public Editor()
    {
        var commonRoot = PathService.CommonDirectory;

        graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = $"{commonRoot}";
        IsMouseVisible = true;
        
        Window.AllowUserResizing = true;
        graphics.PreferredBackBufferWidth = 1600;
        graphics.PreferredBackBufferHeight = 900;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void LoadContent()
    {
        spriteBatch = new SpriteBatch(GraphicsDevice);
        
        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;
        levelEditor = new LevelEditor(Content, screenWidth, screenHeight);
        
        try
        {
            font = Content.Load<SpriteFont>("EditorFont");
        }
        catch
        {
            font = null;
        }
        
        pixel = new Texture2D(GraphicsDevice, 1, 1);
        pixel.SetData([Color.White]);
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        if (showInstructions &&
            keyboardState.IsKeyDown(Keys.Escape) &&
            previousKeyState.IsKeyUp(Keys.Escape))
        {
            Exit();
        }

        // Закрыть инструкции по нажатию Enter или пробела
        if (showInstructions && 
            (keyboardState.IsKeyDown(Keys.Enter) && previousKeyState.IsKeyUp(Keys.Enter) ||
             keyboardState.IsKeyDown(Keys.Space) && previousKeyState.IsKeyUp(Keys.Space)))
        {
            showInstructions = false;
        }

        var mouseState = Mouse.GetState();

        if (!showInstructions)
        {
            levelEditor.Update(gameTime, keyboardState, mouseState);
            if (levelEditor.IsExitRequested)
                Exit();
        }

        previousKeyState = keyboardState;
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black * 0.8f);

        spriteBatch.Begin();
        levelEditor.Draw(spriteBatch, pixel);
        
        // Рисуем инструкции поверх всего
        if (showInstructions && font != null)
        {
            DrawInstructions();
        }
        
        spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawInstructions()
    {
        int screenWidth = GraphicsDevice.Viewport.Width;
        int screenHeight = GraphicsDevice.Viewport.Height;
        
        // Полупрозрачный фон
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.7f);
        
        // Заголовок
        string title = "=== УПРАВЛЕНИЕ РЕДАКТОРОМ ===";
        Vector2 titleSize = font.MeasureString(title);
        Vector2 titlePos = new Vector2((screenWidth - titleSize.X) / 2, 80);
        spriteBatch.DrawString(font, title, titlePos, Color.Yellow);
        
        // Инструкции
        string[] instructions =
        [
            "",
            "ESC - Выход из редактора",
            "",
            "=== РАЗМЕЩЕНИЕ ОБЪЕКТОВ ===",
            "1 - Режим размещения точек спавна",
            "2 - Режим размещения точек защиты",
            "3 - Режим рисования путей",
            "4 - Режим размещения зон строительства",
            "",
            "=== РЕДАКТИРОВАНИЕ БАШЕН ===",
            "T - Открыть/закрыть панель башен",
            "",
            "=== УПРАВЛЕНИЕ ВОЛНАМИ ===",
            "M - Открыть/закрыть Wave Manager",
            "N - Добавить новую волну",
            "D - Удалить выбранную волну",
            "Up/Down - Выбрать волну",
            "E - Добавить врага в выбранную волну",
            "",
            "=== СОХРАНЕНИЕ ===",
            "Ctrl+S - Сохранить карту и волны",
            "Ctrl+P - Упаковать уровень в архив",
            "",
            "Нажмите ENTER или ПРОБЕЛ чтобы начать редактирование..."
        ];
        
        float yOffset = 150;
        foreach (var line in instructions)
        {
            Vector2 lineSize = font.MeasureString(line);
            Vector2 linePos = new Vector2((screenWidth - lineSize.X) / 2, yOffset);
            Color color = line.Contains("Нажмите") ? Color.Lime : Color.White;
            spriteBatch.DrawString(font, line, linePos, color);
            yOffset += 35;
        }
    }
}
