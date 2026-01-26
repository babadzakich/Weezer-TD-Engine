using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using System;
using System.Collections.Generic;

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
    
    private List<UIElement> _allElements;
    private MouseState _previousMouseState;
    private int _screenWidth;
    private int _screenHeight;
    
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
        
        // Панель ресурсов (верх экрана)
        ResourcePanel = new ResourcePanel(
            new Vector2(0, 0),
            new Vector2(screenWidth, 80)
        );
        _allElements.Add(ResourcePanel);
        
        // Кнопка запуска волны (в левом верхнем углу, большая и заметная)
        StartWaveButton = new Button(
            new Vector2(10, 90),
            new Vector2(200, 80),
            "START WAVE"
        );
        StartWaveButton.BackgroundColor = new Color(0, 150, 0);
        StartWaveButton.HoverColor = new Color(0, 200, 0);
        StartWaveButton.OnClick += () => OnStartWaveRequested?.Invoke();
        _allElements.Add(StartWaveButton);
        
        // Панель выбора башен (появляется по клику на зону строительства)
        TowerSelectionPanel = new TowerSelectionPanel(
            new Vector2(100, 100),
            new Vector2(200, 300)
        );
        TowerSelectionPanel.IsVisible = false; // Скрыта по умолчанию
        _allElements.Add(TowerSelectionPanel);
        
        // Панель управления башней (появляется при выборе башни)
        TowerControlPanel = new TowerControlPanel(
            new Vector2(100, 100),
            new Vector2(250, 190)
        );
        TowerControlPanel.OnSellTower += (tower) => OnTowerSellRequested?.Invoke(tower);
        TowerControlPanel.OnUpgradeTower += (tower) => OnTowerUpgradeRequested?.Invoke(tower);
        _allElements.Add(TowerControlPanel);
    }

    /// <summary>
    /// Добавить доступную для постройки башню
    /// </summary>
    public void AddAvailableTower(ITowerBehavior towerBehavior)
    {
        TowerSelectionPanel.AddTowerOption(towerBehavior);
    }

    /// <summary>
    /// <summary>
    /// Показать панель выбора башен возле указанной позиции
    /// </summary>
    public void ShowTowerSelectionAt(Vector2 position)
    {
        // Позиционируем панель рядом с местом клика, но чтобы не выходила за экран
        Vector2 panelPos = position + new Vector2(20, -150);
        
        // Проверяем границы экрана
        if (panelPos.X + TowerSelectionPanel.Size.X > _screenWidth)
            panelPos.X = position.X - TowerSelectionPanel.Size.X - 20;
        if (panelPos.Y < 90)
            panelPos.Y = 90;
        if (panelPos.Y + TowerSelectionPanel.Size.Y > _screenHeight)
            panelPos.Y = _screenHeight - TowerSelectionPanel.Size.Y - 10;
            
        TowerSelectionPanel.Position = panelPos;
        TowerSelectionPanel.IsVisible = true;
        
        // Обновляем позиции кнопок внутри панели
        TowerSelectionPanel.UpdateButtonPositions();
    }
    
    /// <summary>
    /// Скрыть панель выбора башен
    /// </summary>
    public void HideTowerSelection()
    {
        TowerSelectionPanel.IsVisible = false;
        TowerSelectionPanel.DeselectTower();
    }

    /// <summary>
    /// Показать панель управления для выбранной башни
    /// </summary>
    public void ShowTowerControl(Tower tower)
    {
        // Позиционируем панель рядом с башней
        Vector2 panelPos = tower.Position + new Vector2(30, -95);
        
        // Проверяем границы экрана
        if (panelPos.X + TowerControlPanel.Size.X > _screenWidth)
            panelPos.X = tower.Position.X - TowerControlPanel.Size.X - 30;
        if (panelPos.Y < 90)
            panelPos.Y = 90;
        if (panelPos.Y + TowerControlPanel.Size.Y > _screenHeight)
            panelPos.Y = _screenHeight - TowerControlPanel.Size.Y - 10;
            
        TowerControlPanel.Position = panelPos;
        TowerControlPanel.ShowForTower(tower);
        
        // Обновляем информацию об улучшении
        int? nextCost = tower.GetNextUpgradeCost();
        bool isMaxLevel = tower.Definition == null || tower.Definition.UpgradeLevels == null || tower.UpgradeLevel >= tower.Definition.UpgradeLevels.Count;
        bool canAfford = nextCost.HasValue && Money >= nextCost.Value;
        TowerControlPanel.SetUpgradeInfo(nextCost, canAfford, isMaxLevel);
    }

    /// <summary>
    /// Скрыть панель управления башней
    /// </summary>
    public void HideTowerControl()
    {
        TowerControlPanel.Hide();
    }
    /// <summary>
    /// Проверить, можно ли купить башню
    /// </summary>
    public bool CanAffordTower(ITowerBehavior towerBehavior)
    {
        return Money >= towerBehavior.Cost;
    }

    /// <summary>
    /// Купить башню (списать деньги)
    /// </summary>
    public bool PurchaseTower(ITowerBehavior towerBehavior)
    {
        if (CanAffordTower(towerBehavior))
        {
            Money -= towerBehavior.Cost;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Продать башню (вернуть часть денег)
    /// </summary>
    public void SellTower(Tower tower, float refundPercent = 0.7f)
    {
        int refund = (int)(tower.Behavior.Cost * refundPercent);
        Money += refund;
    }

    public void Update(GameTime gameTime)
    {
        MouseState currentMouseState = Mouse.GetState();
        
        // Динамически обновляем состояние кнопок управления башней, если панель открыта
        if (TowerControlPanel.IsVisible && TowerControlPanel.SelectedTower != null)
        {
            var tower = TowerControlPanel.SelectedTower;
            int? nextCost = tower.GetNextUpgradeCost();
            bool isMaxLevel = tower.Definition == null || tower.Definition.UpgradeLevels == null || tower.UpgradeLevel >= tower.Definition.UpgradeLevels.Count;
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

    /// <summary>
    /// Проверить, был ли клик по UI элементу
    /// </summary>
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
