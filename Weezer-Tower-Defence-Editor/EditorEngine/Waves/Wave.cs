using System.Collections.Generic;


namespace EditorEngine.Waves;

public class Wave
{
    public int Index { get; set; }
    public List<EnemySpawn> Spawns { get; } = new();
}
