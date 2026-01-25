namespace EditorEngine.Towers.Types;

/// <summary>
/// Пулемётная башня - близкая атака, быстрая стрельба
/// </summary>
public class MachineGunTower : ITowerConfig
{
    public string Id { get; set; } = "machine_gun";
    public string Name { get; set; } = "Machine gun tower";
    public string ClassName { get; set; }
        = "SimulationEngine.TowerRelated.Behaviors.BasicTowerBehavior";
    public string BulletClassName { get; set; }
        = "SimulationEngine.BulletRelated.Behaviors.BasicBulletBehavior";

    public int Cost { get; set; } = 100;
    public float Range { get; set; } = 75f;
    public float FireRate { get; set; } = 10f;
    public int Damage { get; set; } = 10;
}
