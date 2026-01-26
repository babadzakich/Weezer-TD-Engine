using System.Collections.Generic;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace SimulationEngine.WaveRelated;

public class Wave
{
    public class EnemyGroup
    {
        public System.Type Type { get; set; }
        public int Count { get; set; }
        public SpawnPoint SpawnPoint { get; set; }
        public string EnemyStringId { get; set; }
    }

    public string Id { get; set; }
    public List<EnemyGroup> EnemyGroups { get; private set; }

    public Wave(string id)
    {
        Id = id;
        EnemyGroups = new List<EnemyGroup>();
    }

    public void AddEnemy(System.Type enemyType, int count, SpawnPoint spawnPoint)
    {
        EnemyGroups.Add(new EnemyGroup { 
            Type = enemyType, 
            Count = count, 
            SpawnPoint = spawnPoint 
        });
    }
    
    public void AddEnemy(System.Type enemyType, int count, SpawnPoint spawnPoint, string enemyStringId)
    {
        EnemyGroups.Add(new EnemyGroup { 
            Type = enemyType, 
            Count = count, 
            SpawnPoint = spawnPoint, 
            EnemyStringId = enemyStringId 
        });
    }
}