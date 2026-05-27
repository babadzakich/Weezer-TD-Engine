using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.Network;
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

    public ILobbyDiscovery Discovery { get; set; }
    public System.Collections.Generic.Dictionary<string, LevelLoader.TowerDefinition> TowerDefinitions { get; set; }

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
        if (_selectedBuildZone == null) return;
        if (!_uiManager.CanAffordTower(towerBehavior)) return;

        var towerDef = definition ?? towerBehavior.Definition;
        var zone = _selectedBuildZone;

        _uiManager.HideTowerSelection();
        _selectedBuildZone = null;

        if (Discovery != null)
        {
            // В мультиплеере: шлём событие, локально применим когда получим ответ от хоста
            Discovery.BroadcastTowerPlace(zone.Id, towerDef.Id);
        }
        else
        {
            ApplyTowerPlacement(zone.Id, towerDef.Id);
        }
    }

    /// <summary>Применяет постановку башни локально. Вызывается либо в singleplayer напрямую,
    /// либо по событию OnRemoteTowerPlace от сети.</summary>
    public void ApplyTowerPlacement(string buildZoneId, string towerDefId)
    {
        var zone = _map.BuildZones.Find(z => z.Id == buildZoneId);
        if (zone == null || zone.IsOccupied) return;

        LevelLoader.TowerDefinition def = null;
        TowerDefinitions?.TryGetValue(towerDefId, out def);
        if (def == null) return;

        var behavior = new SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior(
            def,
            new StandardBulletBehavior(25f, 300f, 500f));

        var tower = new Tower(behavior, zone.Position, def);
        _towerController.AddTower(tower);

        // Списываем деньги локально у каждого игрока (общая экономика "по умолчанию")
        if (_uiManager.Money >= def.Cost) _uiManager.Money -= def.Cost;
        zone.Occupy(tower);
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
            Texture = _towerController.GetTowerTexture(targetDefinition)
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
