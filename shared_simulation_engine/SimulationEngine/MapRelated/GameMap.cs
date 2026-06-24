using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.Infrastructure;
using System.Collections.Generic;
using System.IO;

namespace SimulationEngine.MapRelated;

/// <summary>
/// Игровая карта - содержит все элементы: пути, точки спавна, зоны строительства и точки защиты
/// </summary>
public class GameMap
{
    public string Id { get; set; }
    public string Name { get; set; }
    
    public List<SpawnPoint> SpawnPoints { get; private set; }
    public List<DefensePoint> DefensePoints { get; private set; }
    public List<Path> Paths { get; private set; }
    public List<BuildZone> BuildZones { get; private set; }
    
    public int Width { get; set; }
    public int Height { get; set; }

    public Texture2D PathTexture { get; set; }
    public float PathWidth { get; set; } = 40f;

    public GameMap(string id, string name, int width, int height)
    {
        Id = id;
        Name = name;
        Width = width;
        Height = height;
        
        SpawnPoints = new List<SpawnPoint>();
        DefensePoints = new List<DefensePoint>();
        Paths = new List<Path>();
        BuildZones = new List<BuildZone>();
    }

    public void AddSpawnPoint(SpawnPoint spawnPoint)
    {
        SpawnPoints.Add(spawnPoint);
    }

    public void AddDefensePoint(DefensePoint defensePoint)
    {
        DefensePoints.Add(defensePoint);
    }

    public void AddPath(Path path)
    {
        Paths.Add(path);
    }

    public void AddBuildZone(BuildZone buildZone)
    {
        BuildZones.Add(buildZone);
    }

    /// <summary>
    /// Найти свободную зону строительства по позиции
    /// </summary>
    public BuildZone FindAvailableBuildZone(Vector2 position)
    {
        foreach (var zone in BuildZones)
        {
            if (!zone.IsOccupied && zone.Contains(position))
                return zone;
        }
        return null;
    }

    /// <summary>
    /// Получить путь по ID
    /// </summary>
    public Path GetPath(string pathId)
    {
        return Paths.Find(p => p.Id == pathId);
    }

    /// <summary>
    /// Получить путь по ID (алиас для совместимости)
    /// </summary>
    public Path GetPathById(string pathId)
    {
        return GetPath(pathId);
    }

    /// <summary>
    /// Получить точку защиты по ID
    /// </summary>
    public DefensePoint GetDefensePoint(string id)
    {
        return DefensePoints.Find(dp => dp.Id == id);
    }

    /// <summary>
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Рисуем пути (сглаженные сплайном)
        foreach (var path in Paths)
        {
            var pathPoints = path.GetSmoothPath();

            if (PathTexture != null)
            {
                // Рисуем текстурированную дорожку
                for (int i = 0; i < pathPoints.Count - 1; i++)
                {
                    DrawPathSegment(spriteBatch, PathTexture, pathPoints[i], pathPoints[i + 1], PathWidth);
                }
            }
            else
            {
                // Рисуем сглаженный путь (жёлтые линии) - fallback
                for (int i = 0; i < pathPoints.Count - 1; i++)
                {
                    DrawLine(spriteBatch, pixel, pathPoints[i], pathPoints[i + 1], Color.Yellow, 3);
                }
            }
        }

        // Рисуем зоны строительства
        foreach (var zone in BuildZones)
        {
            Color color = zone.IsOccupied ? Color.Red * 0.3f : Color.Green * 0.3f;
            DrawRectangle(spriteBatch, pixel, zone.Position - zone.Size / 2, zone.Size, color);
        }

        // Рисуем точки спавна
        foreach (var spawn in SpawnPoints)
        {
            // spriteBatch.Draw(_spawnpoint, spawn.Position - new Vector2(_spawnpoint.Width / 2, _spawnpoint.Height / 2), Color.White);
            DrawCircle(spriteBatch, pixel, spawn.Position, 10, Color.Red);
        }

        // Рисуем точки защиты
        foreach (var defense in DefensePoints)
        {
            Color color = defense.IsDestroyed ? Color.Gray : Color.Blue;
            DrawCircle(spriteBatch, pixel, defense.Position, 15, color);
        }
    }

    private void DrawPathSegment(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 end, float width)
    {
        Vector2 edge = end - start;
        float angle = (float)System.Math.Atan2(edge.Y, edge.X);
        float length = edge.Length();

        // Рисуем сегмент текстуры, растягивая его по длине участка
        spriteBatch.Draw(texture,
            new Rectangle((int)start.X, (int)start.Y, (int)length + 1, (int)width),
            null, Color.White, angle, new Vector2(0, texture.Height / 2f), SpriteEffects.None, 0);
    }

    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, float thickness)
    {
        Vector2 edge = end - start;
        float angle = (float)System.Math.Atan2(edge.Y, edge.X);
        
        spriteBatch.Draw(pixel,
            new Rectangle((int)start.X, (int)start.Y, (int)edge.Length(), (int)thickness),
            null, color, angle, new Vector2(0, 0.5f), SpriteEffects.None, 0);
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position, Vector2 size, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle((int)position.X, (int)position.Y, (int)size.X, (int)size.Y), color);
    }

    private void DrawCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 position, float radius, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle((int)(position.X - radius), (int)(position.Y - radius), 
            (int)(radius * 2), (int)(radius * 2)), color);
    }
}
