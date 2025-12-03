using Microsoft.Xna.Framework;

namespace SimulationEngine.MapRelated;

/// <summary>
/// Зона где можно строить башни
/// </summary>
public class BuildZone
{
    public Vector2 Position { get; set; }
    public string Id { get; set; }
    public bool IsOccupied { get; set; }
    
    /// <summary>
    /// Размер зоны (для прямоугольной зоны)
    /// </summary>
    public Vector2 Size { get; set; }

    public BuildZone(Vector2 position, string id, Vector2? size = null)
    {
        Position = position;
        Id = id;
        IsOccupied = false;
        Size = size ?? new Vector2(50, 50); // По умолчанию 50x50
    }

    /// <summary>
    /// Проверка, находится ли точка внутри зоны строительства
    /// </summary>
    public bool Contains(Vector2 point)
    {
        return point.X >= Position.X - Size.X / 2 &&
               point.X <= Position.X + Size.X / 2 &&
               point.Y >= Position.Y - Size.Y / 2 &&
               point.Y <= Position.Y + Size.Y / 2;
    }

    /// <summary>
    /// Занять зону башней
    /// </summary>
    public void Occupy()
    {
        IsOccupied = true;
    }

    /// <summary>
    /// Освободить зону
    /// </summary>
    public void Free()
    {
        IsOccupied = false;
    }
}
