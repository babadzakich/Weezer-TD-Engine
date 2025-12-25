namespace EditorEngine.Towers.Types;

/// <summary>
/// Снайперская башня - дальняя атака, медленная перезарядка
/// </summary>
public class SniperTower
{
    public string Id => "sniper";
    public string Name => "Sniper Tower";
    public int Cost => 250;
    public float Range => 300f;
    public float FireRate => 0.5f;
    public int Damage => 50;
}
