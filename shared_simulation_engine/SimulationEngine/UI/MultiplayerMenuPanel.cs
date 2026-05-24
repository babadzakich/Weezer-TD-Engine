using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimulationEngine.UI;

public class MultiplayerMenuPanel
{
    private Button _createLobbyButton;
    private Button _joinLobbyButton;
    private Button _backButton;
    private int _screenWidth;
    private int _screenHeight;

    public event Action OnCreateLobbyClicked;
    public event Action OnJoinLobbyClicked;
    public event Action OnBackClicked;

    public MultiplayerMenuPanel(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        int buttonWidth = 400;
        int buttonHeight = 60;
        int centerX = _screenWidth / 2 - buttonWidth / 2;

        _createLobbyButton = new Button(new Vector2(centerX, 300), new Vector2(buttonWidth, buttonHeight), "Создать лобби");
        _joinLobbyButton = new Button(new Vector2(centerX, 400), new Vector2(buttonWidth, buttonHeight), "Присоединиться");
        _backButton = new Button(new Vector2(centerX, 550), new Vector2(buttonWidth, buttonHeight), "Назад");

        _createLobbyButton.OnClick += () => OnCreateLobbyClicked?.Invoke();
        _joinLobbyButton.OnClick += () => OnJoinLobbyClicked?.Invoke();
        _backButton.OnClick += () => OnBackClicked?.Invoke();
    }

    public void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        _createLobbyButton.Update(gameTime, mouseState, previousMouseState);
        _joinLobbyButton.Update(gameTime, mouseState, previousMouseState);
        _backButton.Update(gameTime, mouseState, previousMouseState);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (font == null) return;

        string title = "СЕТЕВАЯ ИГРА";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 150), Color.Yellow);

        _createLobbyButton.Draw(spriteBatch, pixel, font);
        _joinLobbyButton.Draw(spriteBatch, pixel, font);
        _backButton.Draw(spriteBatch, pixel, font);
    }
}
