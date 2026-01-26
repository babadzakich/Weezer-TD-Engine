using System.Collections.Generic;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace SimulationEngine.WaveRelated;

public class Wave
{
    public string Id { get; set; }
    public Dictionary<System.Type, (int count, SpawnPoint spawnPoint)> Enemies { get; private set; }
    
    // Дополнительная информация для загрузки врагов из уровней
    public Dictionary<System.Type, string> EnemyStringIds { get; private set; }

    public Wave(string id)
    {
        Id = id;
        Enemies = new Dictionary<System.Type, (int count, SpawnPoint spawnPoint)>();
        EnemyStringIds = new Dictionary<System.Type, string>();
    }

    public void AddEnemy(System.Type enemyType, int count, SpawnPoint spawnPoint)
    {
        Enemies.Add(enemyType, (count, spawnPoint));
    }
    
    public void AddEnemy(System.Type enemyType, int count, SpawnPoint spawnPoint, string enemyStringId)
    {
        Enemies.Add(enemyType, (count, spawnPoint));
        EnemyStringIds[enemyType] = enemyStringId;
    }
}