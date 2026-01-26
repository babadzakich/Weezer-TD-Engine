namespace EditorEngine.Towers.Types;

/// <summary>
/// Базовая башня для редактора
/// </summary>
public interface ITowerConfig
{
    string Id { get; set; }
    string Name { get; set; }
    public string ClassName { get; set; }
    public string BulletClassName { get; set; }
    int Cost { get; set; }
    float Range { get; set; }
    float FireRate { get; set; }
    int Damage { get; set; }
}
