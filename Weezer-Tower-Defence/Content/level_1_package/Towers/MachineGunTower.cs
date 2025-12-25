namespace EditorEngine.Towers.Types;

/// <summary>
/// Пулемётная башня - близкая атака, быстрая стрельба
/// </summary>
public class MachineGunTower
{
    public string Id => "machinegun";
    public string Name => "Machine Gun Tower";
    public int Cost => 150;
    public float Range => 100f;
    public float FireRate => 3f;
    public int Damage => 5;
}
