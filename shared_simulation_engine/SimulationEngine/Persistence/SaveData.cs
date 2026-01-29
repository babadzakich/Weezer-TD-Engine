using System;
using System.Collections.Generic;

namespace SimulationEngine.Persistence;

/// <summary>
/// Данные сохранения игры
/// </summary>
[Serializable]
public class SaveData
{
    public string SaveName { get; set; }
    public DateTime SaveTime { get; set; }
    public string Version { get; set; }
    
    // Игровое состояние
    public GameStateData GameState { get; set; }
    
    // Карта
    public MapData Map { get; set; }
    
    // Башни
    public List<TowerData> Towers { get; set; }
    
    // Враги (если нужно сохранять активных врагов)
    public List<EnemyData> Enemies { get; set; }

    public SaveData()
    {
        SaveTime = DateTime.Now;
        Version = "1.0";
        Towers = new List<TowerData>();
        Enemies = new List<EnemyData>();
    }
}

[Serializable]
public class GameStateData
{
    public int Money { get; set; }
    public int Lives { get; set; }
    public int CurrentWave { get; set; }
    public bool IsWaveActive { get; set; }
    public int Score { get; set; }
}

[Serializable]
public class MapData
{
    public string MapId { get; set; }
    public List<string> OccupiedBuildZoneIds { get; set; }
    public Dictionary<string, int> DefensePointHealths { get; set; }

    public MapData()
    {
        OccupiedBuildZoneIds = new List<string>();
        DefensePointHealths = new Dictionary<string, int>();
    }
}

[Serializable]
public class TowerData
{
    public string TowerTypeId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public int UpgradeLevel { get; set; }
    public float FireCooldown { get; set; }
}

[Serializable]
public class EnemyData
{
    public string EnemyTypeId { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public int CurrentHealth { get; set; }
    public int CurrentWaypointIndex { get; set; }
    public float PathProgress { get; set; }
    public string PathId { get; set; }
}
