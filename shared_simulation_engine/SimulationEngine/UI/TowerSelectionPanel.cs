using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.TowerRelated;

namespace SimulationEngine.UI;

/// <summary>
/// Панель для выбора башни для постройки
/// </summary>
public class TowerSelectionPanel : UIElement
{
    public List<TowerButton> TowerButtons { get; private set; }
    public Color BackgroundColor { get; set; }
    public ITowerBehavior SelectedTower { get; private set; }
    public LevelLoader.TowerDefinition SelectedDefinition { get; private set; }

    public event Action<ITowerBehavior, LevelLoader.TowerDefinition> OnTowerSelected;

    public TowerSelectionPanel(Vector2 position, Vector2 size) : base(position, size)
    {
        BackgroundColor = new Color(40, 40, 40, 220);
        TowerButtons = new List<TowerButton>();
    }

    public void AddTowerOption(ITowerBehavior towerBehavior, LevelLoader.TowerDefinition definition, Texture2D icon = null)
    {
        int index = TowerButtons.Count;
        Vector2 buttonSize = new Vector2(Size.X - 20, 80);
        Vector2 buttonPos = Position + new Vector2(10, 10 + index * 90);

        var button = new TowerButton(buttonPos, buttonSize, towerBehavior, definition, icon);
        button.OnClick += () => SelectTower(towerBehavior, definition);
        TowerButtons.Add(button);

        Size = new Vector2(Size.X, 20 + TowerButtons.Count * 90);
    }

    private void SelectTower(ITowerBehavior towerBehavior, LevelLoader.TowerDefinition definition)
    {
        SelectedTower = towerBehavior;
        SelectedDefinition = definition;
        OnTowerSelected?.Invoke(towerBehavior, definition);

        foreach (var btn in TowerButtons)
        {
            btn.IsSelected = btn.TowerBehavior == towerBehavior;
        }
    }

    public void DeselectTower()
    {
        SelectedTower = null;
        SelectedDefinition = null;
        foreach (var btn in TowerButtons)
        {
            btn.IsSelected = false;
        }
    }

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

        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);

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
    public ITowerBehavior TowerBehavior { get; }
    public LevelLoader.TowerDefinition Definition { get; }
    public Texture2D Icon { get; set; }
    public bool IsSelected { get; set; }
    public Color SelectedColor { get; set; }

    private bool _isHovered;

    public TowerButton(Vector2 position, Vector2 size, ITowerBehavior towerBehavior, LevelLoader.TowerDefinition definition, Texture2D icon)
        : base(position, size, "")
    {
        TowerBehavior = towerBehavior;
        Definition = definition;
        Icon = icon;
        SelectedColor = new Color(100, 150, 100);
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible) return;

        Color bgColor = IsSelected
            ? SelectedColor
            : (!IsEnabled ? DisabledColor : (_isHovered ? HoverColor : BackgroundColor));

        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), bgColor);
        DrawBorder(spriteBatch, pixel, Position, Size, IsSelected ? Color.Green : Color.White, IsSelected ? 3 : 2);

        if (font != null)
        {
            Vector2 textPos = Position + new Vector2(10, 10);
            spriteBatch.DrawString(font, Definition?.Name ?? TowerBehavior.Id, textPos, TextColor);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Cost: ${TowerBehavior.Cost}", textPos, Color.Gold);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Range: {TowerBehavior.Range}", textPos, Color.Cyan);
        }
    }

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
