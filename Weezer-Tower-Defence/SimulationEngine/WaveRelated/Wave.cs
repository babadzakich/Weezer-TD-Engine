using System.Collections.Generic;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace SimulationEngine.WaveRelated;

public class Wave
{
    public string Id { get; set; }
    public Dictionary<System.Type, (int count, SpawnPoint spawnPoint)> Enemies { get; private set; }

    public Wave(string id)
    {
        Id = id;
        Enemies = new Dictionary<System.Type, (int count, SpawnPoint spawnPoint)>();
    }

    public void AddEnemy(System.Type enemyType, int count, SpawnPoint spawnPoint)
    {
        Enemies.Add(enemyType, (count, spawnPoint));
    }
}