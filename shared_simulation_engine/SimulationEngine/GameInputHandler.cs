using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;
using SimulationEngine.UI;
using System;
using SimulationEngine.BulletRelated.Behaviors;

namespace SimulationEngine;

public class GameInputHandler
{
    private readonly UIManager _uiManager;
    private readonly GameMap _map;
    private readonly TowerController _towerController;
    
    private MouseState _previousMouseState;
    private BuildZone _selectedBuildZone;
    private Tower _selectedTower;

    public GameInputHandler(UIManager uiManager, GameMap map, TowerController towerController)
    {
        _uiManager = uiManager;
        _map = map;
        _towerController = towerController;
        
        _uiManager.TowerSelectionPanel.OnTowerSelected += OnTowerSelectedFromPanel;
        _uiManager.OnTowerSellRequested += SellTower;
        _uiManager.OnTowerUpgradeRequested += UpgradeTower;
    }

    public void Update()
    {
        MouseState mouseState = Mouse.GetState();
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        
        // Обрабатываем клики на игровом поле (не на UI)
        if (!_uiManager.IsMouseOverUI(mousePos))
        {
            HandleGameFieldInput(mouseState, mousePos);
        }
        
        _previousMouseState = mouseState;
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
        BuildZone buildZone = _map.FindAvailableBuildZone(position);
        
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
        _uiManager.ShowTowerSelectionAt(position);
        _uiManager.HideTowerControl();
    }

    private void ShowTowerControlPanel(Tower tower)
    {
        _selectedTower = tower;
        _selectedBuildZone = null;
        
        // Показываем панель управления башней
        _uiManager.ShowTowerControl(tower);
        _uiManager.HideTowerSelection();
    }

    private void OnTowerSelectedFromPanel(ITowerBehavior towerBehavior, LevelLoader.TowerDefinition definition)
    {
        // Проверяем, что у нас выбрана зона для строительства
        if (_selectedBuildZone == null) return;
        
        // Проверяем, хватает ли денег
        if (!_uiManager.CanAffordTower(towerBehavior))
        {
            // TODO: показать сообщение о недостатке средств
            return;
        }
        
        // Создаём башню с выбранным поведением
        ITowerBehavior behaviorInstance = towerBehavior;
        LevelLoader.TowerDefinition towerDefinition = definition ?? towerBehavior.Definition;

        if (towerBehavior is SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior defBehavior)
        {
            towerDefinition ??= defBehavior.Definition;
            // новый экземпляр, чтобы состояния не пересекались
            behaviorInstance = new SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior(
                towerDefinition,
                new StandardBulletBehavior(25f, 300f, 500f));
        }
        else
        {
            behaviorInstance.Definition = towerDefinition;
        }

        var tower = new Tower(behaviorInstance, _selectedBuildZone.Position, towerDefinition);
        tower.Texture = GameManager.GetInstance().DefaultTowerTexture;
        _towerController.AddTower(tower);
        
        // Списываем деньги и занимаем зону
        _uiManager.PurchaseTower(towerBehavior);
        _selectedBuildZone.Occupy(tower);
        
        // Закрываем панель выбора
        _uiManager.HideTowerSelection();
        _selectedBuildZone = null;
    }

    private Tower FindTowerAtPosition(Vector2 position)
    {
        foreach (var tower in _towerController.towers)
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
        _uiManager.HideTowerSelection();
        _uiManager.HideTowerControl();
        _selectedBuildZone = null;
        _selectedTower = null;
    }

    private void SellTower(Tower tower)
    {
        if (tower == null) return;
        
        // Возвращаем деньги
        _uiManager.SellTower(tower);
        
        // Освобождаем зону строительства
        foreach (var zone in _map.BuildZones)
        {
            if (Vector2.Distance(zone.Position, tower.Position) < 10)
            {
                zone.Free();
                break;
            }
        }
        
        // Удаляем башню
        _towerController.towers.Remove(tower);
        
        // Скрываем панель управления
        _uiManager.HideTowerControl();
        _selectedTower = null;
    }

    private void UpgradeTower(Tower tower, LevelLoader.TowerUpgradeDefinition next)
    {
        if (tower == null) return;
        if (next == null) return;

        var def = tower.Definition;
        if (def == null || def.Upgrades == null || def.Upgrades.Count == 0) return;

        int cost = next.Cost;
        if (_uiManager.Money < cost) return;

        if (string.IsNullOrWhiteSpace(next.TargetTowerId)) return;

        var gameManager = GameManager.GetInstance();
        if (!gameManager.TowerDefinitions.TryGetValue(next.TargetTowerId, out var targetDefinition))
        {
            Console.WriteLine($"Upgrade target '{next.TargetTowerId}' was not found for tower '{def.Id}'.");
            return;
        }

        var upgradedBehavior = TowerBehaviorFactory.CreateTowerBehavior(targetDefinition);
        var upgradedTower = new Tower(upgradedBehavior, tower.Position, targetDefinition)
        {
            Texture = tower.Texture ?? gameManager.DefaultTowerTexture
        };

        _uiManager.Money -= cost;

        int towerIndex = _towerController.towers.IndexOf(tower);
        if (towerIndex >= 0)
        {
            _towerController.towers[towerIndex] = upgradedTower;
        }
        else
        {
            _towerController.AddTower(upgradedTower);
        }

        _selectedTower = upgradedTower;

        // обновляем UI панели управления
        _uiManager.ShowTowerControl(upgradedTower);
    }
}
