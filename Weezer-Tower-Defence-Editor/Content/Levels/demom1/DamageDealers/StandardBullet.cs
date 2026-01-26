namespace EditorEngine.DamageDealers.Types;

/// <summary>
/// Стандартный снаряд для редактора
/// </summary>
public class StandardBullet
{
    public string Id => "standard";
    public string Name => "Standard Bullet";
    public float Speed => 200f;
    public int Damage => 10;
}
