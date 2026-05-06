using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.Sprites;
using SimulationEngine.Infrastructure;
using System;

namespace SimulationEngine.MapRelated;

/// <summary>
/// Точка спавна врагов
/// </summary>
public class SpawnPoint
{
    public Vector2 Position { get; set; }
    public string Id { get; set; }
    
    /// <summary>
    /// К какому пути привязана эта точка спавна
    /// </summary>
    public string PathId { get; set; }

    public SpawnPoint(Vector2 position, string id, string pathId)
    {
        Position = position;
        Id = id;
        PathId = pathId;
    }
}
