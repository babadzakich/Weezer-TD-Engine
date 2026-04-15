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

    public int Money { get => ResourcePanel.Money; set => ResourcePanel.Money = value; }
    public int Lives { get => ResourcePanel.Lives; set => ResourcePanel.Lives = value; }
    public int Wave { get => ResourcePanel.Wave; set => ResourcePanel.Wave = value; }

    public event Action OnStartWaveRequested;
    public event Action<Tower> OnTowerSellRequested;
    public event Action<Tower> OnTowerUpgradeRequested;

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
            "START WAVE");
        StartWaveButton.BackgroundColor = new Color(0, 150, 0);
        StartWaveButton.HoverColor = new Color(0, 200, 0);
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
        TowerControlPanel.OnUpgradeTower += tower => OnTowerUpgradeRequested?.Invoke(tower);
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

        int? nextCost = tower.GetNextUpgradeCost();
        bool isMaxLevel = tower.Definition == null ||
                          tower.Definition.UpgradeLevels == null ||
                          tower.UpgradeLevel >= tower.Definition.UpgradeLevels.Count;
        bool canAfford = nextCost.HasValue && Money >= nextCost.Value;
        TowerControlPanel.SetUpgradeInfo(nextCost, canAfford, isMaxLevel);
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
            int? nextCost = tower.GetNextUpgradeCost();
            bool isMaxLevel = tower.Definition == null ||
                              tower.Definition.UpgradeLevels == null ||
                              tower.UpgradeLevel >= tower.Definition.UpgradeLevels.Count;
            bool canAfford = nextCost.HasValue && Money >= nextCost.Value;
            TowerControlPanel.SetUpgradeInfo(nextCost, canAfford, isMaxLevel);
        }

        foreach (var element in _allElements)
        {
            element.Update(gameTime, currentMouseState, _previousMouseState);
        }

        _previousMouseState = currentMouseState;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        foreach (var element in _allElements)
        {
            element.Draw(spriteBatch, pixel, font);
        }
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
}
