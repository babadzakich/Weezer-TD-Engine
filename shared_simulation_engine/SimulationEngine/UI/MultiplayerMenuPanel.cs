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

    public string PlayerName { get; set; } = "Player";

    private string GetTypedText(KeyboardState current, KeyboardState previous, string currentText)
    {
        foreach (var key in Enum.GetValues<Keys>())
        {
            if (current.IsKeyDown(key) && previous.IsKeyUp(key))
            {
                if (key == Keys.Back)
                {
                    if (currentText.Length > 0)
                        currentText = currentText.Substring(0, currentText.Length - 1);
                }
                else if (key == Keys.Space)
                {
                    if (currentText.Length < 15)
                        currentText += " ";
                }
                else
                {
                    string keyStr = key.ToString();
                    if (keyStr.Length == 1 && currentText.Length < 15)
                    {
                        bool shift = current.IsKeyDown(Keys.LeftShift) || current.IsKeyDown(Keys.RightShift);
                        char c = keyStr[0];
                        if (!shift) c = char.ToLower(c);
                        currentText += c;
                    }
                    else if (keyStr.StartsWith("D") && keyStr.Length == 2 && char.IsDigit(keyStr[1]) && currentText.Length < 15)
                    {
                        currentText += keyStr[1];
                    }
                    else if (keyStr.StartsWith("NumPad") && keyStr.Length == 7 && char.IsDigit(keyStr[6]) && currentText.Length < 15)
                    {
                        currentText += keyStr[6];
                    }
                }
            }
        }
        return currentText;
    }

    public void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState, KeyboardState keyboardState, KeyboardState previousKeyboardState)
    {
        _createLobbyButton.Update(gameTime, mouseState, previousMouseState);
        _joinLobbyButton.Update(gameTime, mouseState, previousMouseState);
        _backButton.Update(gameTime, mouseState, previousMouseState);

        PlayerName = GetTypedText(keyboardState, previousKeyboardState, PlayerName);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (font == null) return;

        string title = "СЕТЕВАЯ ИГРА";
        Vector2 titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title, new Vector2(_screenWidth / 2 - titleSize.X / 2, 80), Color.Yellow);

        // Рисуем поле ввода имени
        Rectangle inputBox = new Rectangle(_screenWidth / 2 - 200, 160, 400, 50);
        spriteBatch.Draw(pixel, inputBox, new Color(40, 40, 40, 200));
        DrawBorder(spriteBatch, pixel, inputBox, 2, Color.White);

        string labelText = "Имя: ";
        string nameText = PlayerName;
        if (DateTime.Now.Millisecond < 500)
            nameText += "|";

        string fullText = labelText + nameText;
        Vector2 fullTextSize = font.MeasureString(fullText);
        spriteBatch.DrawString(font, fullText, new Vector2(_screenWidth / 2 - fullTextSize.X / 2, 175), Color.White);

        _createLobbyButton.Draw(spriteBatch, pixel, font);
        _joinLobbyButton.Draw(spriteBatch, pixel, font);
        _backButton.Draw(spriteBatch, pixel, font);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int thickness, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }
}
