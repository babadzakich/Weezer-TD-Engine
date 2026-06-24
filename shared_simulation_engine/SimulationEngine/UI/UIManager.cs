using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.TowerRelated;

namespace SimulationEngine.UI;

/// <summary>
/// Главный UI менеджер, управляющий всеми UI элементами
/// </summary>
public class UIManager
{
    public ResourcePanel ResourcePanel { get; private set; }
    public TowerSelectionPanel TowerSelectionPanel { get; private set; }
    public TowerControlPanel TowerControlPanel { get; private set; }
    public Button StartWaveButton { get; private set; }

    private readonly List<UIElement> _allElements;
    private MouseState _previousMouseState;
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private bool _connectionLostVisible;
    private int  _connectionLostSeconds;

    private string _notificationText = "";
    private float  _notificationTimer = 0f;

    public int Money { get => ResourcePanel.Money; set => ResourcePanel.Money = value; }
    public int Lives { get => ResourcePanel.Lives; set => ResourcePanel.Lives = value; }
    public int Wave { get => ResourcePanel.Wave; set => ResourcePanel.Wave = value; }
    // Local player's instance id (set by the host/client launcher)
    public string LocalPlayerInstanceId { get; set; } = string.Empty;
    public Func<string, string> ResolvePlayerName { get; set; }

    public event Action OnStartWaveRequested;
    public event Action<Tower> OnTowerSellRequested;
    public event Action<Tower, LevelLoader.TowerUpgradeDefinition> OnTowerUpgradeRequested;

    public UIManager(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _allElements = new List<UIElement>();

        ResourcePanel = new ResourcePanel(
            new Vector2(0, 0),
            new Vector2(screenWidth, 80));
        _allElements.Add(ResourcePanel);

        StartWaveButton = new Button(
            new Vector2(10, 90),
            new Vector2(200, 80),
            "START WAVE")
        {
            BackgroundColor = new Color(0, 150, 0),
            HoverColor = new Color(0, 200, 0)
        };
        StartWaveButton.OnClick += () => OnStartWaveRequested?.Invoke();
        _allElements.Add(StartWaveButton);

        TowerSelectionPanel = new TowerSelectionPanel(
            new Vector2(100, 100),
            new Vector2(200, 300));
        TowerSelectionPanel.IsVisible = false;
        _allElements.Add(TowerSelectionPanel);

        TowerControlPanel = new TowerControlPanel(
            new Vector2(100, 100),
            new Vector2(250, 190));
        TowerControlPanel.OnSellTower += tower => OnTowerSellRequested?.Invoke(tower);
        TowerControlPanel.OnUpgradeTower += (tower, option) => OnTowerUpgradeRequested?.Invoke(tower, option);
        _allElements.Add(TowerControlPanel);
    }

    public void AddAvailableTower(ITowerBehavior towerBehavior, LevelLoader.TowerDefinition definition)
    {
        TowerSelectionPanel.AddTowerOption(towerBehavior, definition);
    }

    public void ShowTowerSelectionAt(Vector2 position)
    {
        Vector2 panelPos = position + new Vector2(20, -150);

        if (panelPos.X + TowerSelectionPanel.Size.X > _screenWidth - 20)
            panelPos.X = position.X - TowerSelectionPanel.Size.X - 20;
        if (panelPos.Y < 90)
            panelPos.Y = 90;
        if (panelPos.Y + TowerSelectionPanel.Size.Y > _screenHeight - 20)
            panelPos.Y = _screenHeight - TowerSelectionPanel.Size.Y - 20;

        TowerSelectionPanel.Position = panelPos;
        TowerSelectionPanel.IsVisible = true;
        TowerSelectionPanel.UpdateButtonPositions();
    }

    public void HideTowerSelection()
    {
        TowerSelectionPanel.IsVisible = false;
        TowerSelectionPanel.DeselectTower();
    }

    public void ShowTowerControl(Tower tower)
    {
        Vector2 panelPos = tower.Position + new Vector2(30, -95);

        if (panelPos.X + TowerControlPanel.Size.X > _screenWidth - 20)
            panelPos.X = tower.Position.X - TowerControlPanel.Size.X - 30;
        if (panelPos.Y < 90)
            panelPos.Y = 90;
        if (panelPos.Y + TowerControlPanel.Size.Y > _screenHeight - 20)
            panelPos.Y = _screenHeight - TowerControlPanel.Size.Y - 20;

        TowerControlPanel.Position = panelPos;
        TowerControlPanel.ShowForTower(tower);
        TowerControlPanel.UpdateButtonPositions();

        var upgrades = tower.Definition?.Upgrades ?? new List<LevelLoader.TowerUpgradeDefinition>();
        bool isMaxLevel = tower.Definition == null ||
                          tower.Definition.Upgrades == null ||
                          tower.Definition.Upgrades.Count == 0;
        bool isOwner = tower.IsOwnedBy(LocalPlayerInstanceId);
        SimulationEngine.Network.OwnershipDebug.Log($"UIManager ShowTowerControl: tower.NetworkId={tower.NetworkId} tower.OwnerInstanceId='{tower.OwnerInstanceId}' LocalPlayerInstanceId='{LocalPlayerInstanceId}' isOwner={isOwner}");
        TowerControlPanel.SetUpgradeInfo(upgrades, Money, isMaxLevel, isOwner);
    }

    public void HideTowerControl()
    {
        TowerControlPanel.Hide();
    }

    public bool CanAffordTower(ITowerBehavior towerBehavior)
    {
        return Money >= towerBehavior.Cost;
    }

    public bool PurchaseTower(ITowerBehavior towerBehavior)
    {
        if (!CanAffordTower(towerBehavior))
            return false;

        Money -= towerBehavior.Cost;
        return true;
    }

    public void SellTower(Tower tower, float refundPercent = 0.7f)
    {
        int refund = (int)(tower.Behavior.Cost * refundPercent);
        Money += refund;
    }

    public void Update(GameTime gameTime)
    {
        MouseState currentMouseState = Mouse.GetState();

        if (TowerControlPanel.IsVisible && TowerControlPanel.SelectedTower != null)
        {
            var tower = TowerControlPanel.SelectedTower;
            var upgrades = tower.Definition?.Upgrades ?? new List<LevelLoader.TowerUpgradeDefinition>();
            bool isMaxLevel = tower.Definition == null ||
                              tower.Definition.Upgrades == null ||
                              tower.Definition.Upgrades.Count == 0;
            bool isOwner = tower.IsOwnedBy(LocalPlayerInstanceId);
            TowerControlPanel.SetUpgradeInfo(upgrades, Money, isMaxLevel, isOwner);
        }

        foreach (var element in _allElements)
        {
            element.Update(gameTime, currentMouseState, _previousMouseState);
        }

        _previousMouseState = currentMouseState;

        if (_notificationTimer > 0f)
            _notificationTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public void ShowNotification(string text, float durationSeconds = 3f)
    {
        _notificationText  = text;
        _notificationTimer = durationSeconds;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        foreach (var element in _allElements)
        {
            element.Draw(spriteBatch, pixel, font);
        }

        if (_notificationTimer > 0f && font != null && !string.IsNullOrEmpty(_notificationText))
            DrawNotification(spriteBatch, pixel, font);

        if (_connectionLostVisible)
            DrawConnectionLostOverlay(spriteBatch, pixel, font);
    }

    public bool IsMouseOverUI(Vector2 mousePosition)
    {
        foreach (var element in _allElements)
        {
            if (element.IsVisible && element.Contains(mousePosition))
                return true;
        }

        return false;
    }

    public void ShowConnectionLost(int secondsLeft)
    {
        _connectionLostVisible = true;
        _connectionLostSeconds = secondsLeft;
    }

    public void HideConnectionLost()
    {
        _connectionLostVisible = false;
    }

    private void DrawNotification(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        var textSize = font.MeasureString(_notificationText);
        float padding = 12f;
        float w = textSize.X + padding * 2;
        float h = textSize.Y + padding * 2;
        float x = (_screenWidth - w) / 2f;
        float y = _screenHeight - h - 20f;

        // Fade out in the last 0.5 seconds
        float alpha = Math.Clamp(_notificationTimer / 0.5f, 0f, 1f);
        byte a = (byte)(200 * alpha);

        spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, (int)w, (int)h), new Color(0, 0, 0, (int)a));
        spriteBatch.DrawString(font, _notificationText,
            new Vector2(x + padding, y + padding),
            new Color(255, 80, 80, (int)a));
    }

    private void DrawConnectionLostOverlay(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        // Semi-transparent black overlay
        spriteBatch.Draw(pixel,
            new Rectangle(0, 0, _screenWidth, _screenHeight),
            new Color(0, 0, 0, 160));

        if (font == null) return;

        string title   = "Соединение потеряно";
        string counter = $"Выход через {_connectionLostSeconds} сек...";

        var titleSize   = font.MeasureString(title);
        var counterSize = font.MeasureString(counter);

        var titlePos   = new Vector2((_screenWidth - titleSize.X) / 2f,   _screenHeight / 2f - 40);
        var counterPos = new Vector2((_screenWidth - counterSize.X) / 2f, _screenHeight / 2f + 10);

        spriteBatch.DrawString(font, title,   titlePos,   Color.White);
        spriteBatch.DrawString(font, counter, counterPos, Color.OrangeRed);
    }
}
