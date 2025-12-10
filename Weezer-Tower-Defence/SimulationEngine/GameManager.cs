using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.UI;
using System;
using SimulationEngine.BulletRelated.Behaviors;

namespace SimulationEngine;

/// <summary>
/// Главный менеджер игры - координирует взаимодействие UI, карты, башен и врагов
/// </summary>
public class GameManager
{
    public UIManager UIManager { get; private set; }
    public GameMap Map { get; private set; }
    public TowerController TowerController { get; private set; }
    
    private MouseState _previousMouseState;
    private BuildZone _selectedBuildZone;
    private Tower _selectedTower;

    public event Action OnGameOver;
    public event Action<int> OnWaveCompleted;

    public GameManager(int screenWidth, int screenHeight, GameMap map, TowerController towerController)
    {
        UIManager = new UIManager(screenWidth, screenHeight);
        Map = map;
        TowerController = towerController;
        
        // Подписываемся на события UI
        UIManager.OnStartWaveRequested += StartWave;
        UIManager.OnTowerSellRequested += SellTower;
        UIManager.OnTowerUpgradeRequested += UpgradeTower;
        UIManager.TowerSelectionPanel.OnTowerSelected += OnTowerSelectedFromPanel;
    }

    public void Update(GameTime gameTime)
    {
        UIManager.Update(gameTime);
        
        MouseState mouseState = Mouse.GetState();
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        
        // Обрабатываем клики на игровом поле (не на UI)
        if (!UIManager.IsMouseOverUI(mousePos))
        {
            HandleGameFieldInput(mouseState, mousePos);
        }
        
        _previousMouseState = mouseState;
        
        // Проверка на поражение
        if (UIManager.Lives <= 0)
        {
            OnGameOver?.Invoke();
        }
    }

    private void HandleGameFieldInput(MouseState mouseState, Vector2 mousePos)
    {
        // Клик левой кнопкой мыши
        if (mouseState.LeftButton == ButtonState.Pressed && 
            _previousMouseState.LeftButton == ButtonState.Released)
        {
            HandleLeftClick(mousePos);
        }
        
        // Клик правой кнопкой - отмена выбора
        if (mouseState.RightButton == ButtonState.Pressed && 
            _previousMouseState.RightButton == ButtonState.Released)
        {
            CancelSelection();
        }
    }

    private void HandleLeftClick(Vector2 position)
    {
        // Сначала проверяем, есть ли башня под курсором
        Tower clickedTower = FindTowerAtPosition(position);
        
        if (clickedTower != null)
        {
            // Кликнули на башню - показываем панель управления
            ShowTowerControlPanel(clickedTower);
            return;
        }
        
        // Проверяем, есть ли зона строительства под курсором
        BuildZone buildZone = Map.FindAvailableBuildZone(position);
        
        if (buildZone != null)
        {
            // Кликнули на пустую зону - показываем панель выбора башен
            ShowTowerSelectionPanel(buildZone, position);
            return;
        }
        
        // Кликнули в пустое место - закрываем все панели
        CancelSelection();
    }

    private void ShowTowerSelectionPanel(BuildZone buildZone, Vector2 position)
    {
        _selectedBuildZone = buildZone;
        _selectedTower = null;
        
        // Показываем панель выбора башен возле клика
        UIManager.ShowTowerSelectionAt(position);
        UIManager.HideTowerControl();
    }

    private void ShowTowerControlPanel(Tower tower)
    {
        _selectedTower = tower;
        _selectedBuildZone = null;
        
        // Показываем панель управления башней
        UIManager.ShowTowerControl(tower);
        UIManager.HideTowerSelection();
    }

    private void OnTowerSelectedFromPanel(TowerConfig towerConfig)
    {
        // Проверяем, что у нас выбрана зона для строительства
        if (_selectedBuildZone == null) return;
        
        // Проверяем, хватает ли денег
        if (!UIManager.CanAffordTower(towerConfig))
        {
            // TODO: показать сообщение о недостатке средств
            return;
        }
        
        // Создаём башню в выбранной зоне
        var tower = new Tower(towerConfig, _selectedBuildZone.Position);
        TowerController.AddTower(tower);
        
        // Списываем деньги и занимаем зону
        UIManager.PurchaseTower(towerConfig);
        _selectedBuildZone.Occupy();
        
        // Закрываем панель выбора
        UIManager.HideTowerSelection();
        _selectedBuildZone = null;
    }

    private Tower FindTowerAtPosition(Vector2 position)
    {
        foreach (var tower in TowerController.towers)
        {
            float distance = Vector2.Distance(tower.Position, position);
            if (distance < 30) // Радиус клика по башне
            {
                return tower;
            }
        }
        return null;
    }

    private void CancelSelection()
    {
        UIManager.HideTowerSelection();
        UIManager.HideTowerControl();
        _selectedBuildZone = null;
        _selectedTower = null;
    }

    private void SellTower(Tower tower)
    {
        if (tower == null) return;
        
        // Возвращаем деньги
        UIManager.SellTower(tower);
        
        // Освобождаем зону строительства
        foreach (var zone in Map.BuildZones)
        {
            if (Vector2.Distance(zone.Position, tower.Position) < 10)
            {
                zone.Free();
                break;
            }
        }
        
        // Удаляем башню
        TowerController.towers.Remove(tower);
        
        // Скрываем панель управления
        UIManager.HideTowerControl();
        _selectedTower = null;
    }

    private void UpgradeTower(Tower tower)
    {
        // TODO: Реализовать систему апгрейдов
        // Пока заглушка
    }

    private void StartWave()
    {
        // TODO: Запуск волны врагов
        UIManager.Wave++;
    }

    public void AddMoney(int amount)
    {
        UIManager.Money += amount;
    }

    public void RemoveLife()
    {
        UIManager.Lives--;
    }
}
