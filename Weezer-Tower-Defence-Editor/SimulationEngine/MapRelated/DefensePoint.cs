using System.IO;
using Microsoft.Xna.Framework;

namespace SimulationEngine.MapRelated;

/// <summary>
/// Точка защиты - куда не должны дойти враги
/// </summary>
public class DefensePoint
{
    public Vector2 Position { get; set; }
    public string Id { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }

    public DefensePoint(Vector2 position, string id, int maxHealth = 100)
    {
        Position = position;
        Id = id;
        MaxHealth = maxHealth;
        Health = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;
        if (Health < 0) Health = 0;
    }

    public bool IsDestroyed => Health <= 0;

    public override string ToString()
    {
        return $"DefencePoint(Id = {Id} Position={Position}, Health={Health})";
    }
}
