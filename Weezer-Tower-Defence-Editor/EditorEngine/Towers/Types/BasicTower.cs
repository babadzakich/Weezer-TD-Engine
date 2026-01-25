namespace EditorEngine.Towers.Types;

/// <summary>
/// Базовая башня для редактора
/// </summary>
public class BasicTower : ITowerConfig
{
    public string Id => "basic";
    public string Name => "Basic Tower";
    public string ClassName => "SimulationEngine.TowerRelated.Behaviors.BasicTowerBehavior";
    public string BulletClassName => "SimulationEngine.BulletRelated.Behaviors.BasicBulletBehavior";
    public int Cost => 100;
    public float Range => 150f;
    public float FireRate => 1f;
    public int Damage => 10;
}
