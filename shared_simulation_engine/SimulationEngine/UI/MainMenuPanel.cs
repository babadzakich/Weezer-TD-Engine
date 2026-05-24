using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimulationEngine.UI;

public class MainMenuPanel
{
    private Button _singlePlayerButton;
    private Button _multiPlayerButton;
    private int _screenWidth;
    private int _screenHeight;

    public event Action OnSingleplayerClicked;
    public event Action OnMultiplayerClicked;

    public MainMenuPanel(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        int buttonWidth = 300;
        int buttonHeight = 60;
        int centerX = _screenWidth / 2 - buttonWidth / 2;

        _singlePlayerButton = new Button(new Vector2(centerX, 300), new Vector2(buttonWidth, buttonHeight), "Одиночная игра");
        _multiPlayerButton = new Button(new Vector2(centerX, 400), new Vector2(buttonWidth, buttonHeight), "Сетевая игра");

        _singlePlayerButton.OnClick += () => OnSingleplayerClicked?.Invoke();
        _multiPlayerButton.OnClick += () => OnMultiplayerClicked?.Invoke();
    }

    public void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        _singlePlayerButton.Update(gameTime, mouseState, previousMouseState);
        _multiPlayerButton.Update(gameTime, mouseState, previousMouseState);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (font == null) return;

        string title = "WEEZER TOWER DEFENCE";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 150), Color.Yellow);

        _singlePlayerButton.Draw(spriteBatch, pixel, font);
        _multiPlayerButton.Draw(spriteBatch, pixel, font);
    }
}
