namespace EditorEngine.DamageDealers.Types;

/// <summary>
/// Лазерный луч для редактора
/// </summary>
public class LaserBeam
{
    public string Id => "laser";
    public string Name => "Laser Beam";
    public float Speed => 0f; // Мгновенный
    public int Damage => 15;
}
