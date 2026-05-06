using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.TowerRelated;
using System;
using System.Collections.Generic;

namespace SimulationEngine.UI;

/// <summary>
/// Панель управления башней (апгрейд, продажа)
/// </summary>
public class TowerControlPanel : UIElement
{
    public Tower SelectedTower { get; private set; }
    public Color BackgroundColor { get; set; }
    
    private Button _sellButton;
    private Button _showUpgradeButton;
    private readonly List<Button> _upgradeOptionButtons = new();
    private List<LevelLoader.TowerUpgradeDefinition> _upgradeOptions = new();
    private bool _isMaxLevel;
    private bool _isUpgradeListOpen;
    
    public event Action<Tower> OnSellTower;
    public event Action<Tower, LevelLoader.TowerUpgradeDefinition> OnUpgradeTower;

    public TowerControlPanel(Vector2 position, Vector2 size) : base(position, size)
    {
        BackgroundColor = new Color(40, 40, 60, 220);
        IsVisible = false;

        // Кнопка продажи
        _sellButton = new Button(
            position + new Vector2(10, size.Y - 50),
            new Vector2(size.X / 2 - 15, 40),
            "Sell"
        )
        {
            BackgroundColor = new Color(150, 50, 50),
            HoverColor = new Color(180, 60, 60)
        };
        _sellButton.OnClick += () => OnSellTower?.Invoke(SelectedTower);
        
        _showUpgradeButton = new Button(
            position + new Vector2(size.X / 2 + 5, size.Y - 50),
            new Vector2(size.X / 2 - 15, 40),
            "Upgrades"
        )
        {
            BackgroundColor = new Color(50, 100, 150),
            HoverColor = new Color(60, 120, 180)
        };
        _showUpgradeButton.OnClick += ToggleUpgradeList;
    }

    public void ShowForTower(Tower tower)
    {
        SelectedTower = tower;
        IsVisible = true;
        _isUpgradeListOpen = false;
    }

    public void SetUpgradeInfo(List<LevelLoader.TowerUpgradeDefinition> upgradeOptions, int currentMoney, bool isMaxLevel)
    {
        _isMaxLevel = isMaxLevel;
        _upgradeOptions = upgradeOptions ?? new List<LevelLoader.TowerUpgradeDefinition>();
        RebuildUpgradeButtons(currentMoney);

        if (_isMaxLevel)
        {
            _showUpgradeButton.IsVisible = false;
            _isUpgradeListOpen = false;
        }
        else
        {
            _showUpgradeButton.IsVisible = true;
            _showUpgradeButton.Text = _isUpgradeListOpen ? "Hide Upgrades" : "Show Upgrades";
            _showUpgradeButton.IsEnabled = _upgradeOptions.Count > 0;
        }

        UpdateButtonPositions();
    }

    public void Hide()
    {
        SelectedTower = null;
        IsVisible = false;
        _isUpgradeListOpen = false;
    }

    public override bool Contains(Vector2 point)
    {
        if (base.Contains(point))
        {
            return true;
        }

        if (!_isUpgradeListOpen)
        {
            return false;
        }

        foreach (var button in _upgradeOptionButtons)
        {
            if (button.IsVisible && button.Contains(point))
            {
                return true;
            }
        }

        return false;
    }
    
    /// <summary>
    /// Обновить позиции кнопок при изменении позиции панели
    /// </summary>
    public void UpdateButtonPositions()
    {
        if (_isMaxLevel)
        {
            // Центрируем кнопку продажи, если апгрейд недоступен
            _sellButton.Position = Position + new Vector2((Size.X - _sellButton.Size.X) / 2, Size.Y - 50);
        }
        else
        {
            _sellButton.Position = Position + new Vector2(10, Size.Y - 50);
            _showUpgradeButton.Position = Position + new Vector2(Size.X / 2 + 5, Size.Y - 50);
        }

        for (int i = 0; i < _upgradeOptionButtons.Count; i++)
        {
            _upgradeOptionButtons[i].Position = new Vector2(
                _showUpgradeButton.Position.X,
                Position.Y + Size.Y + 5 + i * 45);
        }
    }

    public override void Update(GameTime gameTime, MouseState mouseState, MouseState previousMouseState)
    {
        if (!IsVisible) return;

        _sellButton.Update(gameTime, mouseState, previousMouseState);
        _showUpgradeButton.Update(gameTime, mouseState, previousMouseState);

        if (_isUpgradeListOpen)
        {
            var buttonsSnapshot = _upgradeOptionButtons.ToArray();
            foreach (var button in buttonsSnapshot)
            {
                button.Update(gameTime, mouseState, previousMouseState);
            }
        }
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
            spriteBatch.DrawString(font, $"Type: {SelectedTower.Behavior.Id}", textPos, Color.Cyan);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Range: {SelectedTower.Behavior.Range}", textPos, Color.Yellow);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Fire Rate: {SelectedTower.Behavior.FireRate}/s", textPos, Color.Orange);
            textPos.Y += 20;
            spriteBatch.DrawString(font, $"Level: {SelectedTower.UpgradeLevel + 1}", textPos, Color.LightGreen);
            textPos.Y += 20;
            if (_isMaxLevel)
            {
                spriteBatch.DrawString(font, "Upgrades: MAX", textPos, Color.Gray);
            }
            else
            {
                spriteBatch.DrawString(font, $"Upgrades: {_upgradeOptions.Count}", textPos, Color.Gold);
            }
        }

        _sellButton.Draw(spriteBatch, pixel, font);
        _showUpgradeButton.Draw(spriteBatch, pixel, font);

        if (_isUpgradeListOpen)
        {
            var buttonsSnapshot = _upgradeOptionButtons.ToArray();
            foreach (var button in buttonsSnapshot)
            {
                button.Draw(spriteBatch, pixel, font);
            }
        }
    }

    private void ToggleUpgradeList()
    {
        if (_isMaxLevel || _upgradeOptions.Count == 0)
        {
            return;
        }

        _isUpgradeListOpen = !_isUpgradeListOpen;
        _showUpgradeButton.Text = _isUpgradeListOpen ? "Hide Upgrades" : "Show Upgrades";
    }

    private void RebuildUpgradeButtons(int currentMoney)
    {
        _upgradeOptionButtons.Clear();

        foreach (var option in _upgradeOptions)
        {
            var optionButton = new Button(
                _showUpgradeButton.Position,
                _showUpgradeButton.Size,
                $"{option.TargetTowerId} (${option.Cost})")
            {
                BackgroundColor = new Color(35, 70, 110),
                HoverColor = new Color(50, 95, 145),
                IsEnabled = currentMoney >= option.Cost
            };

            var capturedOption = option;
            optionButton.OnClick += () =>
            {
                _isUpgradeListOpen = false;
                _showUpgradeButton.Text = "Show Upgrades";
                OnUpgradeTower?.Invoke(SelectedTower, capturedOption);
            };

            _upgradeOptionButtons.Add(optionButton);
        }
    }
}
