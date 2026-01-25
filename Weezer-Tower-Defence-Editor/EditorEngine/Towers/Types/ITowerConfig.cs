namespace EditorEngine.Towers.Types;

/// <summary>
/// Базовая башня для редактора
/// </summary>
public interface ITowerConfig
{
    string Id { get; }
    string Name { get; }
    public string ClassName { get;  }
    public string BulletClassName { get;  }
    int Cost { get; }
    float Range { get; }
    float FireRate { get; }
    int Damage { get; }
}
