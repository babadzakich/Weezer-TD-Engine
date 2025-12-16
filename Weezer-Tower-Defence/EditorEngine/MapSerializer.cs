using SimulationEngine.MapRelated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using PathIO = System.IO.Path;
using GamePath = SimulationEngine.MapRelated.Path;

namespace EditorEngine;

public static class MapSerializer
{
    private class SerializedMap
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("spawnPoints")]
        public List<SerializedSpawnPoint> SpawnPoints { get; set; } = new();

        [JsonPropertyName("defensePoints")]
        public List<SerializedDefensePoint> DefensePoints { get; set; } = new();

        [JsonPropertyName("paths")]
        public List<SerializedPath> Paths { get; set; } = new();
    }

    private class SerializedSpawnPoint
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }
    }

    private class SerializedDefensePoint
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("maxHealth")]
        public int MaxHealth { get; set; }
    }

    private class SerializedPath
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("defensePointId")]
        public string DefensePointId { get; set; }

        [JsonPropertyName("useSmoothPath")]
        public bool UseSmoothPath { get; set; }

        [JsonPropertyName("splineResolution")]
        public int SplineResolution { get; set; }

        [JsonPropertyName("waypoints")]
        public List<Vector2Data> Waypoints { get; set; } = new();
    }

    private class Vector2Data
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        public Vector2Data() { }
        public Vector2Data(Vector2 v) { X = v.X; Y = v.Y; }

        public Vector2 ToVector2() => new(X, Y);
    }

    public static void SaveMap(GameMap map, string filePath)
    {
        try
        {
            // Create directory if it doesn't exist
            string directory = PathIO.GetDirectoryName(filePath);
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            var serialized = new SerializedMap
            {
                Id = map.Id,
                Name = map.Name,
                Width = map.Width,
                Height = map.Height
            };

            // Serialize spawn points
            foreach (var spawn in map.SpawnPoints)
            {
                serialized.SpawnPoints.Add(new SerializedSpawnPoint
                {
                    Id = spawn.Id,
                    X = spawn.Position.X,
                    Y = spawn.Position.Y
                });
            }

            // Serialize defense points
            foreach (var defense in map.DefensePoints)
            {
                serialized.DefensePoints.Add(new SerializedDefensePoint
                {
                    Id = defense.Id,
                    X = defense.Position.X,
                    Y = defense.Position.Y,
                    MaxHealth = defense.MaxHealth
                });
            }

            // Serialize paths
            foreach (var gamePath in map.Paths)
            {
                var serializedPath = new SerializedPath
                {
                    Id = gamePath.Id,
                    DefensePointId = gamePath.DefensePointId,
                    UseSmoothPath = gamePath.UseSmoothPath,
                    SplineResolution = gamePath.SplineResolution,
                    Waypoints = gamePath.Waypoints.Select(w => new Vector2Data(w)).ToList()
                };
                serialized.Paths.Add(serializedPath);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(serialized, options);
            System.IO.File.WriteAllText(filePath, json);

            Console.WriteLine($"Map saved to {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving map: {ex.Message}");
        }
    }

    public static GameMap LoadMap(string filePath)
    {
        try
        {
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine($"Map file not found: {filePath}");
                return null;
            }

            string json = System.IO.File.ReadAllText(filePath);
            var serialized = JsonSerializer.Deserialize<SerializedMap>(json);

            var map = new GameMap(serialized.Id, serialized.Name, serialized.Width, serialized.Height);

            // Deserialize spawn points
            foreach (var spawn in serialized.SpawnPoints)
            {
                var spawnPoint = new SpawnPoint(new Vector2(spawn.X, spawn.Y), spawn.Id, "");
                map.AddSpawnPoint(spawnPoint);
            }

            // Deserialize defense points
            foreach (var defense in serialized.DefensePoints)
            {
                var defensePoint = new DefensePoint(new Vector2(defense.X, defense.Y), defense.Id, defense.MaxHealth);
                map.AddDefensePoint(defensePoint);
            }

            // Deserialize paths
            foreach (var pathData in serialized.Paths)
            {
                var newPath = new GamePath(pathData.Id, pathData.DefensePointId, pathData.UseSmoothPath, pathData.SplineResolution);
                foreach (var waypoint in pathData.Waypoints)
                {
                    newPath.AddWaypoint(waypoint.ToVector2());
                }
                map.AddPath(newPath);
            }

            Console.WriteLine($"Map loaded from {filePath}");
            return map;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading map: {ex.Message}");
            return null;
        }
    }
}
