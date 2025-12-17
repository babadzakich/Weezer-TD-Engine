namespace EditorEngine.Towers.Types;

/// <summary>
/// Базовая башня для редактора
/// </summary>
public class BasicTower
{
    public string Id => "basic";
    public string Name => "Basic Tower";
    public int Cost => 100;
    public float Range => 150f;
    public float FireRate => 1f;
    public int Damage => 10;
}
