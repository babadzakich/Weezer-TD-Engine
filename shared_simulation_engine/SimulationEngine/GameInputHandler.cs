using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.Network;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;
using SimulationEngine.UI;
using SimulationEngine.BulletRelated.Behaviors;

namespace SimulationEngine;

public class GameInputHandler
{
    private readonly UIManager _uiManager;
    private readonly GameMap _map;
    private readonly TowerController _towerController;
    private readonly IGameRequestSender _requestSender;
    
    private MouseState _previousMouseState;
    private BuildZone _selectedBuildZone;
    private Tower _selectedTower;

    public event Action<TowerPlacedEvent> TowerPlaced;
    public event Action<TowerRemovedEvent> TowerRemoved;
    public event Action<TowerUpgradedEvent> TowerUpgraded;

    public GameInputHandler(UIManager uiManager, GameMap map, TowerController towerController, IGameRequestSender requestSender = null)
    {
        _uiManager = uiManager;
        _map = map;
        _towerController = towerController;
        _requestSender = requestSender;

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
        if (!_uiManager.CanAffordTower(towerBehavior))
        {
            return;
        }

        var towerDefinition = definition ?? towerBehavior.Definition;

        if (_requestSender != null)
        {
            var request = new BuildTowerRequest
            {
                RequesterId = "client",
                ZoneId = _selectedBuildZone.Id,
                TowerDefinitionId = towerDefinition?.Id ?? string.Empty
            };
            _ = _requestSender.SendRequestAsync(request, CancellationToken.None);
            _uiManager.HideTowerSelection();
            _selectedBuildZone = null;
            return;
        }

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
        tower.Texture = GameManager.GetInstance().DefaultTowerTexture;
        _towerController.AddTower(tower);

        _uiManager.PurchaseTower(towerBehavior);
        _selectedBuildZone.Occupy(tower);
        _uiManager.HideTowerSelection();
        _selectedBuildZone = null;

        TowerPlaced?.Invoke(new TowerPlacedEvent
        {
            TowerId = tower.Id,
            ZoneId = tower.ZoneId,
            BehaviorId = tower.Definition?.Id ?? string.Empty,
            Owner = "master",
            Cost = tower.Behavior.Cost
        });
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

        if (_requestSender != null)
        {
            var request = new SellTowerRequest
            {
                RequesterId = "client",
                TowerId = tower.Id
            };
            _ = _requestSender.SendRequestAsync(request, CancellationToken.None);
            _uiManager.HideTowerControl();
            _selectedTower = null;
            return;
        }

        _uiManager.SellTower(tower);
        foreach (var zone in _map.BuildZones)
        {
            if (Vector2.Distance(zone.Position, tower.Position) < 10)
            {
                zone.Free();
                break;
            }
        }
        _towerController.towers.Remove(tower);
        _uiManager.HideTowerControl();
        _selectedTower = null;

        TowerRemoved?.Invoke(new TowerRemovedEvent
        {
            TowerId = tower.Id,
            ZoneId = tower.ZoneId,
            Refund = (int)(tower.Behavior.Cost * 0.7f)
        });
    }

    private void UpgradeTower(Tower tower, LevelLoader.TowerUpgradeDefinition next)
    {
        if (tower == null) return;
        if (next == null) return;

        if (_requestSender != null)
        {
            var request = new UpgradeTowerRequest
            {
                RequesterId = "client",
                TowerId = tower.Id,
                TargetTowerId = next.TargetTowerId
            };
            _ = _requestSender.SendRequestAsync(request, CancellationToken.None);
            _uiManager.HideTowerControl();
            _selectedTower = null;
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
        _uiManager.ShowTowerControl(upgradedTower);

        TowerUpgraded?.Invoke(new TowerUpgradedEvent
        {
            TowerId = tower.Id,
            BehaviorId = next.TargetTowerId,
            PrevLevel = tower.UpgradeLevel,
            Level = tower.UpgradeLevel + 1,
            Cost = cost
        });
    }
}
