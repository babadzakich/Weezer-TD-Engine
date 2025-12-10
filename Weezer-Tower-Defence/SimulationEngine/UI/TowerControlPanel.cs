using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.TowerRelated;
using System;

namespace SimulationEngine.UI;

/// <summary>
/// Панель управления башней (апгрейд, продажа)
/// </summary>
public class TowerControlPanel : UIElement
{
    public Tower SelectedTower { get; private set; }
    public Color BackgroundColor { get; set; }
    
    private Button _sellButton;
    private Button _upgradeButton;
    
    public event Action<Tower> OnSellTower;
    public event Action<Tower> OnUpgradeTower;

    public TowerControlPanel(Vector2 position, Vector2 size) : base(position, size)
    {
        BackgroundColor = new Color(40, 40, 60, 220);
        IsVisible = false;
        
        // Кнопка продажи
        _sellButton = new Button(
            position + new Vector2(10, size.Y - 50),
            new Vector2(size.X / 2 - 15, 40),
            "Sell"
        );
        _sellButton.BackgroundColor = new Color(150, 50, 50);
        _sellButton.HoverColor = new Color(180, 60, 60);
        _sellButton.OnClick += () => OnSellTower?.Invoke(SelectedTower);
        
        // Кнопка апгрейда
        _upgradeButton = new Button(
            position + new Vector2(size.X / 2 + 5, size.Y - 50),
            new Vector2(size.X / 2 - 15, 40),
            "Upgrade"
        );
        _upgradeButton.BackgroundColor = new Color(50, 100, 150);
        _upgradeButton.HoverColor = new Color(60, 120, 180);
        _upgradeButton.OnClick += () => OnUpgradeTower?.Invoke(SelectedTower);
    }

    public void ShowForTower(Tower tower)
    {
        SelectedTower = tower;
        IsVisible = true;
    }

    public void Hide()
    {
        SelectedTower = null;
        IsVisible = false;
    }
    
    /// <summary>
    /// Обновить позиции кнопок при изменении позиции панели
    /// </summary>
    public void UpdateButtonPositions()
    {
        _sellButton.Position = Position + new Vector2(10, Size.Y - 50);
        _upgradeButton.Position = Position + new Vector2(Size.X / 2 + 5, Size.Y - 50);
    }

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        if (!IsVisible) return;

        _sellButton.Update(gameTime, mouseState, previousMouseState);
        _upgradeButton.Update(gameTime, mouseState, previousMouseState);
    }

    public override void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        if (!IsVisible || SelectedTower == null) return;

        // Фон панели
        spriteBatch.Draw(pixel, new Rectangle((int)Position.X, (int)Position.Y, (int)Size.X, (int)Size.Y), BackgroundColor);

        if (font != null)
        {
            Vector2 textPos = Position + new Vector2(10, 10);
            spriteBatch.DrawString(font, "Tower Info", textPos, Color.White);
            textPos.Y += 25;
            spriteBatch.DrawString(font, $"Type: {SelectedTower.Config.Id}", textPos, Color.Cyan);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Range: {SelectedTower.Config.Range}", textPos, Color.Yellow);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Fire Rate: {SelectedTower.Config.FireRate}/s", textPos, Color.Orange);
        }

        _sellButton.Draw(spriteBatch, pixel, font);
        _upgradeButton.Draw(spriteBatch, pixel, font);
    }
}
