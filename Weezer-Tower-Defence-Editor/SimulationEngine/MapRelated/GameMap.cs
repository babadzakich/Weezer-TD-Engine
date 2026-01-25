using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

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

    private void logMap()
    {
        foreach (var spawn in this.SpawnPoints)
        {
            Console.WriteLine(spawn);
        }
        foreach (var defense in this.DefensePoints)
        {
            Console.WriteLine(defense);
        }
        foreach (var path in this.Paths)
        {
            Console.WriteLine(path);
        }
        foreach (var buildZone in this.BuildZones)
        {
            Console.WriteLine(buildZone);
        }
    }

    public void AddSpawnPoint(SpawnPoint spawnPoint)
    {
        SpawnPoints.Add(spawnPoint);
        this.logMap();
    }

    public void AddDefensePoint(DefensePoint defensePoint)
    {
        DefensePoints.Add(defensePoint);
        this.logMap();
    }

    public void AddPath(Path path)
    {
        Paths.Add(path);
        this.logMap();
    }

    public void AddBuildZone(BuildZone buildZone)
    {
        BuildZones.Add(buildZone);
        this.logMap();
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
    /// Отрисовка карты (для отладки)
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Рисуем пути (сглаженные сплайном)
        foreach (var path in Paths)
        {
            var pathPoints = path.GetSmoothPath();

            // Рисуем сглаженный путь (жёлтые линии)
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                DrawLine(spriteBatch, pixel, pathPoints[i], pathPoints[i + 1], Color.Yellow, 3);
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
            DrawCircle(spriteBatch, pixel, spawn.Position, 10, Color.Red);
        }

        // Рисуем точки защиты
        foreach (var defense in DefensePoints)
        {
            Color color = defense.IsDestroyed ? Color.Gray : Color.Blue;
            DrawCircle(spriteBatch, pixel, defense.Position, 15, color);
        }
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
