using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.TowerRelated;
using System;
using System.Collections.Generic;

namespace SimulationEngine.UI;

/// <summary>
/// Панель для выбора башни для постройки
/// </summary>
public class TowerSelectionPanel : UIElement
{
    public List<TowerButton> TowerButtons { get; private set; }
    public Color BackgroundColor { get; set; }
    public TowerConfig SelectedTower { get; private set; }
    
    public event Action<TowerConfig> OnTowerSelected;

    public TowerSelectionPanel(Vector2 position, Vector2 size) : base(position, size)
    {
        BackgroundColor = new Color(40, 40, 40, 220);
        TowerButtons = new List<TowerButton>();
    }

    public void AddTowerOption(TowerConfig towerConfig, Texture2D icon = null)
    {
        int index = TowerButtons.Count;
        Vector2 buttonPos = Position + new Vector2(10, 10 + index * 90);
        Vector2 buttonSize = new Vector2(Size.X - 20, 80);
        
        var button = new TowerButton(buttonPos, buttonSize, towerConfig, icon);
        button.OnClick += () => SelectTower(towerConfig);
        TowerButtons.Add(button);
    }

    private void SelectTower(TowerConfig towerConfig)
    {
        SelectedTower = towerConfig;
        OnTowerSelected?.Invoke(towerConfig);
        
        // Подсвечиваем выбранную кнопку
        foreach (var btn in TowerButtons)
        {
            btn.IsSelected = (btn.TowerConfig == towerConfig);
        }
    }

    public void DeselectTower()
    {
        SelectedTower = null;
        foreach (var btn in TowerButtons)
        {
            btn.IsSelected = false;
        }
    }
    
    /// <summary>
    /// Обновить позиции кнопок при изменении позиции панели
    /// </summary>
    public void UpdateButtonPositions()
    {
        for (int i = 0; i < TowerButtons.Count; i++)
        {
            TowerButtons[i].Position = Position + new Vector2(10, 10 + i * 90);
        }
    }

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        if (!IsVisible) return;

        foreach (var button in TowerButtons)
        {
            button.Update(gameTime, mouseState, previousMouseState);
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible) return;

        // Фон панели
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);

        // Кнопки башен
        foreach (var button in TowerButtons)
        {
            button.Draw(spriteBatch, pixel, font);
        }
    }
}

/// <summary>
/// Кнопка для выбора башни
/// </summary>
public class TowerButton : Button
{
    public TowerConfig TowerConfig { get; }
    public Texture2D Icon { get; set; }
    public bool IsSelected { get; set; }
    public Color SelectedColor { get; set; }

    public TowerButton(Vector2 position, Vector2 size, TowerConfig towerConfig, Texture2D icon) 
        : base(position, size, "")
    {
        TowerConfig = towerConfig;
        Icon = icon;
        SelectedColor = new Color(100, 150, 100);
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible) return;

        Color bgColor = IsSelected ? SelectedColor : 
                        (!IsEnabled ? DisabledColor : 
                        (_isHovered ? HoverColor : BackgroundColor));
        
        // Фон кнопки
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), bgColor);
        
        // Рамка
        DrawBorder(spriteBatch, pixel, Position, Size, IsSelected ? Color.Green : Color.White, IsSelected ? 3 : 2);

        if (font != null)
        {
            Vector2 textPos = Position + new Vector2(10, 10);
            spriteBatch.DrawString(font, TowerConfig.Id, textPos, TextColor);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Cost: ${TowerConfig.Cost}", textPos, Color.Gold);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Range: {TowerConfig.Range}", textPos, Color.Cyan);
        }
    }

    private bool _isHovered;

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        base.Update(gameTime, mouseState, previousMouseState);
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        _isHovered = Contains(mousePos);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position, Vector2 size, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)position.Y, (int)size.X, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)(position.Y + size.Y - thickness), (int)size.X, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)position.Y, thickness, (int)size.Y), color);
        spriteBatch.Draw(pixel, new Rectangle((int)(position.X + size.X - thickness), (int)position.Y, thickness, (int)size.Y), color);
    }
}
