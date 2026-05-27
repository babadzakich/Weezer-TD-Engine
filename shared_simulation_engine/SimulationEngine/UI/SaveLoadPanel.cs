using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.UI;
using System;
using System.Collections.Generic;

namespace SimulationEngine.Persistence;

/// <summary>
/// UI панель для сохранения и загрузки игры
/// </summary>
public class SaveLoadPanel : UIElement
{
    public enum Mode
    {
        Save,
        Load
    }

    public Mode CurrentMode { get; set; }
    public Color BackgroundColor { get; set; }
    
    private List<Button> _saveButtons;
    private Button _closeButton;
    private Button _newSaveButton;
    private TextInput _saveNameInput;
    private SaveManager _saveManager;
    private List<SaveInfo> _saves;
    
    public event Action<string> OnSaveRequested;
    public event Action<string> OnLoadRequested;
    public event Action OnClose;

    public SaveLoadPanel(Vector2 position, Vector2 size, SaveManager saveManager) 
        : base(position, size)
    {
        BackgroundColor = new Color(20, 20, 20, 240);
        _saveManager = saveManager;
        _saveButtons = new List<Button>();
        IsVisible = false;
        
        // Кнопка закрытия
        _closeButton = new Button(
            position + new Vector2(size.X - 110, 10),
            new Vector2(100, 40),
            "Close"
        );
        _closeButton.OnClick += () => OnClose?.Invoke();
        
        // Поле ввода имени сохранения (только для режима сохранения)
        _saveNameInput = new TextInput(
            position + new Vector2(10, size.Y - 100),
            new Vector2(size.X - 140, 40),
            "Enter save name..."
        );
        
        // Кнопка создания нового сохранения
        _newSaveButton = new Button(
            position + new Vector2(size.X - 120, size.Y - 100),
            new Vector2(110, 40),
            "Save"
        );
        _newSaveButton.OnClick += HandleNewSave;
        
        RefreshSaveList();
    }

    public void Show(Mode mode)
    {
        CurrentMode = mode;
        IsVisible = true;
        RefreshSaveList();
    }

    public void Hide()
    {
        IsVisible = false;
    }

    private void RefreshSaveList()
    {
        _saves = _saveManager.GetAllSaves();
        _saveButtons.Clear();
        
        for (int i = 0; i < _saves.Count && i < 8; i++) // Максимум 8 сохранений на экране
        {
            var save = _saves[i];
            var button = new Button(
                Position + new Vector2(10, 60 + i * 60),
                new Vector2(Size.X - 20, 50),
                save.GetDisplayText()
            );
            
            if (CurrentMode == Mode.Load)
            {
                button.OnClick += () => OnLoadRequested?.Invoke(save.SaveName);
            }
            else
            {
                button.OnClick += () => OverwriteSave(save.SaveName);
            }
            
            _saveButtons.Add(button);
        }
    }

    private void HandleNewSave()
    {
        if (CurrentMode == Mode.Save && !string.IsNullOrWhiteSpace(_saveNameInput.Text))
        {
            OnSaveRequested?.Invoke(_saveNameInput.Text);
            _saveNameInput.Clear();
        }
    }

    private void OverwriteSave(string saveName)
    {
        if (CurrentMode == Mode.Save)
        {
            OnSaveRequested?.Invoke(saveName);
        }
    }

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        if (!IsVisible) return;

        _closeButton.Update(gameTime, mouseState, previousMouseState);
        
        if (CurrentMode == Mode.Save)
        {
            _saveNameInput.Update(gameTime, mouseState, previousMouseState);
            _newSaveButton.Update(gameTime, mouseState, previousMouseState);
        }
        
        foreach (var button in _saveButtons)
        {
            button.Update(gameTime, mouseState, previousMouseState);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible) return;

        // Фон панели
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);

        if (font != null)
        {
            string title = CurrentMode == Mode.Save ? "Save Game" : "Load Game";
            spriteBatch.DrawString(font, title, Position + new Vector2(10, 10), Color.White);
        }

        _closeButton.Draw(spriteBatch, pixel, font);
        
        if (CurrentMode == Mode.Save)
        {
            _saveNameInput.Draw(spriteBatch, pixel, font);
            _newSaveButton.Draw(spriteBatch, pixel, font);
        }
        
        foreach (var button in _saveButtons)
        {
            button.Draw(spriteBatch, pixel, font);
        }
    }
}

/// <summary>
/// Простое текстовое поле для ввода имени сохранения
/// </summary>
public class TextInput : UIElement
{
    public string Text { get; set; }
    public string Placeholder { get; set; }
    public Color BackgroundColor { get; set; }
    public Color TextColor { get; set; }
    public int MaxLength { get; set; }
    public bool AllowDigitsAndDots { get; set; }

    private bool _isFocused;
    private KeyboardState _previousKeyState;

    public TextInput(Vector2 position, Vector2 size, string placeholder) : base(position, size)
    {
        Text = "";
        Placeholder = placeholder;
        BackgroundColor = new Color(60, 60, 60);
        TextColor = Color.White;
        MaxLength = 30;
    }

    public void Clear()
    {
        Text = "";
    }

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        if (!IsVisible) return;

        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        
        if (mouseState.LeftButton == ButtonState.Pressed && 
            previousMouseState.LeftButton == ButtonState.Released)
        {
            _isFocused = Contains(mousePos);
        }

        if (_isFocused)
        {
            KeyboardState keyState = Keyboard.GetState();
            Keys[] pressedKeys = keyState.GetPressedKeys();

            foreach (Keys key in pressedKeys)
            {
                if (_previousKeyState.IsKeyUp(key))
                {
                    if (key == Keys.Back && Text.Length > 0)
                    {
                        Text = Text.Substring(0, Text.Length - 1);
                    }
                    else if (key == Keys.Space && Text.Length < MaxLength)
                    {
                        Text += " ";
                    }
                    else if (Text.Length < MaxLength)
                    {
                        string keyString = key.ToString();
                        if (keyString.Length == 1)
                        {
                            char c = keyState.IsKeyDown(Keys.LeftShift) || keyState.IsKeyDown(Keys.RightShift)
                                ? char.ToUpper(keyString[0])
                                : char.ToLower(keyString[0]);
                            Text += c;
                        }
                        else if (AllowDigitsAndDots)
                        {
                            if (key >= Keys.D0 && key <= Keys.D9)
                                Text += (char)('0' + (key - Keys.D0));
                            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                                Text += (char)('0' + (key - Keys.NumPad0));
                            else if (key == Keys.OemPeriod || key == Keys.Decimal)
                                Text += '.';
                        }
                    }
                }
            }

            _previousKeyState = keyState;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible) return;

        Color bgColor = _isFocused ? new Color(80, 80, 80) : BackgroundColor;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), bgColor);

        if (font != null)
        {
            string displayText = string.IsNullOrEmpty(Text) ? Placeholder : Text;
            Color textColor = string.IsNullOrEmpty(Text) ? Color.Gray : TextColor;
            
            Vector2 textPos = Position + new Vector2(5, (Size.Y - font.LineSpacing) / 2);
            spriteBatch.DrawString(font, displayText, textPos, textColor);
        }

        // Рамка
        Color borderColor = _isFocused ? Color.Yellow : Color.Gray;
        DrawBorder(spriteBatch, pixel, borderColor);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Color color)
    {
        int thickness = 2;
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)(Position.Y + Size.Y - thickness), (int)Size.X, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, thickness, (int)Size.Y), color);
        spriteBatch.Draw(pixel, new Rectangle((int)(Position.X + Size.X - thickness), (int)Position.Y, thickness, (int)Size.Y), color);
    }
}
