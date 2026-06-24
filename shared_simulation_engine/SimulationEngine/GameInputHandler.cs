using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;
using SimulationEngine.UI;
using System;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.Network;

namespace SimulationEngine;

public class GameInputHandler
{
    private readonly UIManager _uiManager;
    private readonly GameMap _map;
    private readonly TowerController _towerController;

    private MouseState _previousMouseState;
    private BuildZone _selectedBuildZone;
    private Tower _selectedTower;

    /// <summary>Set to true for multiplayer clients (actions go to master instead of applying locally).</summary>
    public bool IsNetworkClient { get; set; } = false;
    public GameSyncManager SyncManager { get; set; }

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
        // If another player claimed our selected build zone, close the panel immediately.
        if (IsNetworkClient && _selectedBuildZone != null && _selectedBuildZone.IsOccupied)
        {
            _uiManager.ShowNotification("Зона уже занята другим игроком!", 3f);
            _uiManager.HideTowerSelection();
            _selectedBuildZone = null;
        }

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
        Console.WriteLine($"[Owner] Clicked tower: NetworkId={tower.NetworkId} OwnerInstanceId='{tower.OwnerInstanceId}' LocalPlayerInstanceId='{_uiManager.LocalPlayerInstanceId}'");
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

        LevelLoader.TowerDefinition towerDefinition = definition ?? towerBehavior.Definition;
        string owner = _uiManager.LocalPlayerInstanceId ?? string.Empty;
        int cost = towerDefinition?.Cost ?? towerBehavior.Cost;
        string behaviorId = !string.IsNullOrWhiteSpace(towerDefinition?.Id)
            ? towerDefinition.Id
            : towerBehavior.Id;
        string zoneId = _selectedBuildZone.Id;

        if (IsNetworkClient && SyncManager != null)
        {
            if (string.IsNullOrEmpty(owner))
            {
                Console.WriteLine($"[Owner] Client placing tower: LocalPlayerInstanceId is not set, cannot determine owner. Tower placement aborted.");
                _uiManager.HideTowerSelection();
                _selectedBuildZone = null;
                return;
            }
            if (_selectedBuildZone.IsOccupied)
            {
                Console.WriteLine($"[Owner] Client placing tower ABORTED: zone '{zoneId}' was occupied by another player while build panel was open.");
                _uiManager.ShowNotification("Зона уже занята другим игроком!", 3f);
                _uiManager.HideTowerSelection();
                _selectedBuildZone = null;
                return;
            }
            SimulationEngine.Network.OwnershipDebug.Log($"Client OnTowerSelected: owner='{owner}' LocalPlayerInstanceId='{_uiManager.LocalPlayerInstanceId}'");
            Console.WriteLine($"[Owner] Client placing tower: owner='{owner}' behaviorId='{behaviorId}' LocalPlayerInstanceId='{_uiManager.LocalPlayerInstanceId}'");
            // Client: send placement request to master, don't apply locally
            // Use a temporary negative ID; master will assign the real one
            SyncManager.RequestTowerPlace(zoneId, behaviorId, owner, cost, -1);
            _uiManager.HideTowerSelection();
            _selectedBuildZone = null;
            return;
        }

        // Host / singleplayer: apply locally
        ITowerBehavior behaviorInstance = towerBehavior;
        if (towerBehavior is SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior defBehavior)
        {
            towerDefinition ??= defBehavior.Definition;
            behaviorInstance = new SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior(
                towerDefinition,
                new StandardBulletBehavior(25f, 300f, 500f));
        }
        else
        {
            behaviorInstance.Definition = towerDefinition;
        }

        var tower = new Tower(behaviorInstance, _selectedBuildZone.Position, towerDefinition);
        tower.OwnerInstanceId = owner;
        SimulationEngine.Network.OwnershipDebug.Log($"Host OnTowerSelected: local tower created. NetworkId={tower.NetworkId} OwnerInstanceId='{tower.OwnerInstanceId}'");
        _towerController.AddTower(tower); // assigns NetworkId

        _uiManager.PurchaseTower(towerBehavior);
        _selectedBuildZone.Occupy(tower);

        // Record for multiplayer broadcast (host mode)
        SyncManager?.RecordTowerPlaced(tower.NetworkId, zoneId, behaviorId, owner, cost);

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
        if (!tower.IsOwnedBy(_uiManager.LocalPlayerInstanceId))
        {
            Console.WriteLine("Attempt to sell tower by non-owner ignored.");
            _uiManager.HideTowerControl();
            return;
        }

        string zoneId = "";
        foreach (var zone in _map.BuildZones)
        {
            if (Vector2.Distance(zone.Position, tower.Position) < 10)
            {
                zoneId = zone.Id;
                break;
            }
        }

        if (IsNetworkClient && SyncManager != null)
        {
            SyncManager.RequestTowerRemove(tower.NetworkId, zoneId);
            _uiManager.HideTowerControl();
            _selectedTower = null;
            return;
        }

        // Host / singleplayer: apply locally
        int refund = (int)((tower.Definition?.Cost ?? 0) * 0.5f);
        _uiManager.SellTower(tower);

        foreach (var zone in _map.BuildZones)
        {
            if (Vector2.Distance(zone.Position, tower.Position) < 10)
            {
                zone.Free();
                break;
            }
        }

        SyncManager?.RecordTowerRemoved(tower.NetworkId, zoneId, refund);
        _towerController.towers.Remove(tower);

        _uiManager.HideTowerControl();
        _selectedTower = null;
    }

    private void UpgradeTower(Tower tower, LevelLoader.TowerUpgradeDefinition next)
    {
        if (tower == null || next == null) return;

        if (!tower.IsOwnedBy(_uiManager.LocalPlayerInstanceId))
        {
            Console.WriteLine("Attempt to upgrade tower by non-owner ignored.");
            _uiManager.HideTowerControl();
            return;
        }

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

        if (IsNetworkClient && SyncManager != null)
        {
            SyncManager.RequestTowerUpgrade(tower.NetworkId, targetDefinition.Id, tower.UpgradeLevel, tower.UpgradeLevel + 1, cost);
            _uiManager.HideTowerControl();
            _selectedTower = null;
            return;
        }

        // Host / singleplayer: apply locally
        var upgradedBehavior = TowerBehaviorFactory.CreateTowerBehavior(targetDefinition);
        var upgradedTower = new Tower(upgradedBehavior, tower.Position, targetDefinition)
        {
            NetworkId       = tower.NetworkId,
            OwnerInstanceId = tower.OwnerInstanceId,
            Texture         = _towerController.GetTowerTexture(targetDefinition),
        };

        _uiManager.Money -= cost;

        int towerIndex = _towerController.towers.IndexOf(tower);
        if (towerIndex >= 0)
            _towerController.towers[towerIndex] = upgradedTower;
        else
            _towerController.AddTower(upgradedTower);

        SyncManager?.RecordTowerUpgraded(tower.NetworkId, targetDefinition.Id, tower.UpgradeLevel, tower.UpgradeLevel + 1, cost);

        _selectedTower = upgradedTower;
        _uiManager.ShowTowerControl(upgradedTower);
    }
}
