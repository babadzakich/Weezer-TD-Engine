using Microsoft.Xna.Framework;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using System.Collections.Generic;

namespace SimulationEngine.Persistence;

/// <summary>
/// Конвертер игровых объектов в данные для сохранения и обратно
/// </summary>
public static class SaveConverter
{
    /// <summary>
    /// Создать SaveData из текущего состояния игры
    /// </summary>
    public static SaveData CreateSaveData(
        GameManager gameManager,
        GameMap map,
        TowerController towerController)
    {
        var saveData = new SaveData();
        
        // Состояние игры
        saveData.GameState = new GameStateData
        {
            Money = gameManager.UIManager.Money,
            Lives = gameManager.UIManager.Lives,
            CurrentWave = gameManager.UIManager.Wave,
            IsWaveActive = false, // TODO: добавить это в GameManager
            Score = 0 // TODO: добавить систему очков
        };
        
        // Данные карты
        saveData.Map = new MapData
        {
            MapId = map.Id
        };
        
        // Сохраняем занятые зоны строительства
        foreach (var zone in map.BuildZones)
        {
            if (zone.IsOccupied)
            {
                saveData.Map.OccupiedBuildZoneIds.Add(zone.Id);
            }
        }
        
        // Сохраняем здоровье точек защиты
        foreach (var defensePoint in map.DefensePoints)
        {
            saveData.Map.DefensePointHealths[defensePoint.Id] = defensePoint.Health;
        }
        
        // Сохраняем башни
        foreach (var tower in towerController.towers)
        {
            saveData.Towers.Add(new TowerData
            {
                TowerTypeId = tower.Behavior.Id,
                PositionX = tower.Position.X,
                PositionY = tower.Position.Y,
                UpgradeLevel = tower.UpgradeLevel,
                FireCooldown = 0 // Можно не сохранять, не критично
            });
        }
        
        return saveData;
    }

    /// <summary>
    /// Загрузить состояние игры из SaveData
    /// </summary>
    public static void LoadFromSaveData(
        SaveData saveData,
        GameManager gameManager,
        GameMap map,
        TowerController towerController,
        Dictionary<string, ITowerBehavior> towerBehaviors) // Реестр доступных типов башен
    {
        if (saveData == null) return;
        
        // Восстанавливаем состояние игры
        if (saveData.GameState != null)
        {
            gameManager.UIManager.Money = saveData.GameState.Money;
            gameManager.UIManager.Lives = saveData.GameState.Lives;
            gameManager.UIManager.Wave = saveData.GameState.CurrentWave;
        }
        
        // Восстанавливаем состояние карты
        if (saveData.Map != null)
        {
            // Восстанавливаем здоровье точек защиты
            foreach (var kvp in saveData.Map.DefensePointHealths)
            {
                var defensePoint = map.GetDefensePoint(kvp.Key);
                if (defensePoint != null)
                {
                    defensePoint.Health = kvp.Value;
                }
            }
            
            // Восстанавливаем занятость зон строительства
            foreach (var zoneId in saveData.Map.OccupiedBuildZoneIds)
            {
                var zone = map.BuildZones.Find(z => z.Id == zoneId);
                if (zone != null)
                {
                    zone.Occupy();
                }
            }
        }
        
        // Очищаем существующие башни
        towerController.towers.Clear();
        
        // Восстанавливаем башни
        foreach (var towerData in saveData.Towers)
        {
            if (towerBehaviors.TryGetValue(towerData.TowerTypeId, out ITowerBehavior behavior))
            {
                var tower = new Tower(
                    behavior,
                    new Vector2(towerData.PositionX, towerData.PositionY)
                );
                towerController.AddTower(tower);
            }
        }
    }

    /// <summary>
    /// Создать реестр доступных типов башен
    /// </summary>
    public static Dictionary<string, ITowerBehavior> CreateTowerBehaviorRegistry(params ITowerBehavior[] behaviors)
    {
        var registry = new Dictionary<string, ITowerBehavior>();
        
        foreach (var behavior in behaviors)
        {
            registry[behavior.Id] = behavior;
        }
        
        return registry;
    }
}
