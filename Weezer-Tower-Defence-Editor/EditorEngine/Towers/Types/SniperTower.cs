namespace EditorEngine.Towers.Types;

/// <summary>
/// Снайперская башня - дальняя атака, медленная перезарядка
/// </summary>
public class SniperTower : ITowerConfig 
{
    public string Id => "sniper_tower";
    public string Name => "Sniper tower";
    public string ClassName => "SimulationEngine.TowerRelated.Behaviors.BasicTowerBehavior";
    public string BulletClassName => "SimulationEngine.BulletRelated.Behaviors.BasicBulletBehavior";
    public int Cost => 100;
    public float Range => 900f;
    public float FireRate => 1f;
    public int Damage => 1000;
}
