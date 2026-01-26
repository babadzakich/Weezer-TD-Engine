using EditorEngine.Towers.Types;

namespace EditorEngine.Towers.Types;
public class BasicTower : ITowerConfig
{
    public string Id { get; set; } = "basic";
    public string Name { get; set; } = "Basic Tower";
    public string ClassName { get; set; }
        = "SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior";
    public string BulletClassName { get; set; }
        = "SimulationEngine.BulletRelated.Behaviors.BasicBulletBehavior";

    public int Cost { get; set; } = 100;
    public float Range { get; set; } = 150f;
    public float FireRate { get; set; } = 1f;
    public int Damage { get; set; } = 10;
}
