namespace EditorEngine.Towers.Types;

/// <summary>
/// Снайперская башня - дальняя атака, медленная перезарядка
/// </summary>
public class SniperTower : ITowerConfig
{
    public string Id { get; set; } = "sniper_tower";
    public string Name { get; set; } = "Sniper tower";
    public string ClassName { get; set; }
        = "SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior";
    public string BulletClassName { get; set; }
        = "SimulationEngine.BulletRelated.Behaviors.BasicBulletBehavior";

    public int Cost { get; set; } = 100;
    public float Range { get; set; } = 900f;
    public float FireRate { get; set; } = 1f;
    public int Damage { get; set; } = 1000;
}
