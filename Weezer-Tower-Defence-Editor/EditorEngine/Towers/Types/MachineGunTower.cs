namespace EditorEngine.Towers.Types;

/// <summary>
/// Пулемётная башня - близкая атака, быстрая стрельба
/// </summary>
public class MachineGunTower : ITowerConfig
{
    public string Id => "machine_gun";
    public string Name => "Machine gun tower";
    public string ClassName => "SimulationEngine.TowerRelated.Behaviors.BasicTowerBehavior";
    public string BulletClassName => "SimulationEngine.BulletRelated.Behaviors.BasicBulletBehavior";
    public int Cost => 100;
    public float Range => 75f;
    public float FireRate => 10f;
    public int Damage => 10;
}
