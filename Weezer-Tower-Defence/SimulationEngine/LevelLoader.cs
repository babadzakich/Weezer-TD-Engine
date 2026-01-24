using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Linq;
using SimulationEngine.MapRelated;
using SimulationEngine.WaveRelated;
using System.Text.Json.Serialization;
using IOPath = System.IO.Path;
using GamePath = SimulationEngine.MapRelated.Path;

namespace SimulationEngine;

/// <summary>
/// Загрузчик уровня из упакованного архива
/// </summary>
public class LevelLoader
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public class LoadedLevel
    {
        public GameMap Map { get; set; }
        public List<WaveData> Waves { get; set; }
        public Dictionary<string, EnemyDefinition> EnemyDefinitions { get; set; }
        public Dictionary<string, TowerDefinition> TowerDefinitions { get; set; }
        public Dictionary<string, DamageDealerDefinition> DamageDealerDefinitions { get; set; }
    }

    public class WaveData
    {
        public int Index { get; set; }
        public List<EnemySpawnData> Spawns { get; set; } = new();
    }

    public class EnemySpawnData
    {
        public string EnemyTypeId { get; set; }
        public string SpawnPointId { get; set; }
        public int Count { get; set; }
    }

    public class EnemyDefinition
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string BehaviorId { get; set; }
        public int BaseHealth { get; set; }
        public float BaseSpeed { get; set; }
        public int Damage { get; set; }
    }

    public class TowerDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public List<UpgradeLevelData> UpgradeLevels { get; set; } = new();
    }

    public class UpgradeLevelData
    {
        public int UpgradeCost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public int Damage { get; set; } // пока что не используем
    }

    public class DamageDealerDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    /// <summary>
    /// Загрузить уровень из архива
    /// </summary>
    public static LoadedLevel LoadFromArchive(string archivePath)
    {
        // Создаём временную папку для распаковки
        string tempDir = IOPath.Combine(IOPath.GetTempPath(), "WeezerTD_Level_" + Guid.NewGuid().ToString());
        
        try
        {
            // Распаковываем архив
            Console.WriteLine($"Extracting level archive: {archivePath} to {tempDir}");
            ZipFile.ExtractToDirectory(archivePath, tempDir);
            Console.WriteLine($"Extracted level to: {tempDir}");

            var level = new LoadedLevel
            {
                EnemyDefinitions = new Dictionary<string, EnemyDefinition>(),
                TowerDefinitions = new Dictionary<string, TowerDefinition>(),
                DamageDealerDefinitions = new Dictionary<string, DamageDealerDefinition>()
            };

            // 1. Загружаем карту
            string mapFile = Directory.GetFiles(tempDir, "*.json", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(f => !IOPath.GetFileName(f).Contains(".waves."));
            
            if (mapFile == null)
                throw new Exception("Map file not found in archive");

            level.Map = LoadMap(mapFile);
            Console.WriteLine($"Loaded map: {level.Map.Name} (ID: {level.Map.Id})");

            // 2. Загружаем волны
            string wavesFile = IOPath.Combine(tempDir, $"{level.Map.Id}.waves.json");
            if (!File.Exists(wavesFile))
            {
                // Попробуем найти любой файл с .waves.json если по ID не совпало
                wavesFile = Directory.GetFiles(tempDir, "*.waves.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }

            if (wavesFile != null && File.Exists(wavesFile))
            {
                level.Waves = LoadWaves(wavesFile);
                Console.WriteLine($"Loaded {level.Waves.Count} waves from {wavesFile}");
            }
            else
            {
                level.Waves = new List<WaveData>();
                Console.WriteLine("No waves file found");
            }

            // 3. Загружаем определения врагов
            string enemiesDir = IOPath.Combine(tempDir, "Enemies");
            if (Directory.Exists(enemiesDir))
            {
                LoadEnemyDefinitions(enemiesDir, level.EnemyDefinitions);
                Console.WriteLine($"Loaded {level.EnemyDefinitions.Count} enemy definitions");
            }

            // 4. Загружаем определения башен
            string towersDir = IOPath.Combine(tempDir, "Towers");
            if (Directory.Exists(towersDir))
            {
                LoadTowerDefinitions(towersDir, level.TowerDefinitions);
                Console.WriteLine($"Loaded {level.TowerDefinitions.Count} tower definitions");
            }

            // 5. Загружаем определения снарядов
            string ddDir = IOPath.Combine(tempDir, "DamageDealers");
            if (Directory.Exists(ddDir))
            {
                LoadDamageDealerDefinitions(ddDir, level.DamageDealerDefinitions);
                Console.WriteLine($"Loaded {level.DamageDealerDefinitions.Count} damage dealer definitions");
            }

            return level;
        }
        finally
        {
            // Удаляем временную папку
            try
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to cleanup temp directory: {ex.Message}");
            }
        }
    }

    private static GameMap LoadMap(string mapFilePath)
    {
        string json = File.ReadAllText(mapFilePath);
        var serializedMap = JsonSerializer.Deserialize<SerializedMap>(json, _jsonOptions);

        var map = new GameMap(serializedMap.Id, serializedMap.Name, serializedMap.Width, serializedMap.Height);

        Console.WriteLine($"Map '{map.Id}' deserialized. Found {serializedMap.SpawnPoints.Count} spawns, {serializedMap.DefensePoints.Count} defense points, {serializedMap.Paths.Count} paths, {serializedMap.BuildZones.Count} build zones.");

        // Загружаем точки спавна
        foreach (var sp in serializedMap.SpawnPoints)
        {
            map.SpawnPoints.Add(new SpawnPoint(
                new Microsoft.Xna.Framework.Vector2(sp.X, sp.Y),
                sp.Id,
                "" // pathId пока пустой, привяжется позже
            ));
        }

        // Загружаем точки защиты
        foreach (var dp in serializedMap.DefensePoints)
        {
            map.DefensePoints.Add(new DefensePoint(
                new Microsoft.Xna.Framework.Vector2(dp.X, dp.Y),
                dp.Id
            ));
        }

        // Загружаем пути
        foreach (var p in serializedMap.Paths)
        {
            var points = p.Waypoints.Select(pt => new Microsoft.Xna.Framework.Vector2(pt.X, pt.Y)).ToList();
            
            // Пытаемся найти SpawnPoint, который находится в начале пути (первая вейпоинт)
            var startPos = points.FirstOrDefault();
            var spawnPoint = map.SpawnPoints.FirstOrDefault(sp => Microsoft.Xna.Framework.Vector2.Distance(sp.Position, startPos) < 5f);
            
            // Если не нашли по позиции, берем первый попавшийся (для совместимости со старыми картами)
            if (spawnPoint == null)
                spawnPoint = map.SpawnPoints.FirstOrDefault();
                
            string spawnPointId = spawnPoint?.Id ?? "";
            
            var path = new GamePath(spawnPointId, p.DefensePointId, useSmoothPath: p.UseSmoothPath);
            path.Id = p.Id; // Устанавливаем ID пути
            
            foreach (var point in points)
            {
                path.AddWaypoint(point);
            }
            map.Paths.Add(path);
            
            // Связываем SpawnPoint с этим путём
            if (spawnPoint != null)
            {
                spawnPoint.PathId = path.Id;
                Console.WriteLine($"Linked SpawnPoint {spawnPoint.Id} to Path {path.Id}");
            }
        }

        // Загружаем зоны строительства
        if (serializedMap.BuildZones != null)
        {
            foreach (var bz in serializedMap.BuildZones)
            {
                map.BuildZones.Add(new BuildZone(
                    new Microsoft.Xna.Framework.Vector2(bz.X, bz.Y),
                    bz.Id,
                    new Microsoft.Xna.Framework.Vector2(bz.SizeX, bz.SizeY)
                ));
            }
        }

        return map;
    }

    private static List<WaveData> LoadWaves(string wavesFilePath)
    {
        string json = File.ReadAllText(wavesFilePath);
        var waveSet = JsonSerializer.Deserialize<WaveSet>(json, _jsonOptions);
        
        if (waveSet?.Waves == null)
            return new List<WaveData>();
        
        // Конвертируем SerializedWave в WaveData
        return waveSet.Waves.Select(w => new WaveData
        {
            Index = w.Index,
            Spawns = w.Spawns.Select(s => new EnemySpawnData
            {
                EnemyTypeId = s.EnemyTypeId,
                SpawnPointId = s.SpawnPointId,
                Count = s.Count
            }).ToList()
        }).ToList();
    }

    private static void LoadEnemyDefinitions(string enemiesDir, Dictionary<string, EnemyDefinition> definitions)
    {
        var jsonFiles = Directory.GetFiles(enemiesDir, "*.json");
        
        foreach (var file in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<EnemyDefinition>(json, _jsonOptions);
                
                if (def != null && !string.IsNullOrEmpty(def.Id))
                {
                    definitions[def.Id] = def;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load enemy definition from {file}: {ex.Message}");
            }
        }
    }

    private static void LoadTowerDefinitions(string towersDir, Dictionary<string, TowerDefinition> definitions)
    {
        var jsonFiles = Directory.GetFiles(towersDir, "*.json");
        
        foreach (var file in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<TowerDefinition>(json, _jsonOptions);
                
                if (def != null && !string.IsNullOrEmpty(def.Id))
                {
                    definitions[def.Id] = def;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load tower definition from {file}: {ex.Message}");
            }
        }
    }

    private static void LoadDamageDealerDefinitions(string ddDir, Dictionary<string, DamageDealerDefinition> definitions)
    {
        var jsonFiles = Directory.GetFiles(ddDir, "*.json");
        
        foreach (var file in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<DamageDealerDefinition>(json, _jsonOptions);
                
                if (def != null && !string.IsNullOrEmpty(def.Id))
                {
                    definitions[def.Id] = def;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load damage dealer definition from {file}: {ex.Message}");
            }
        }
    }

    // Классы для десериализации
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

        [JsonPropertyName("buildZones")]
        public List<SerializedBuildZone> BuildZones { get; set; } = new();
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
        public List<SerializedPoint> Waypoints { get; set; } = new();
    }

    private class SerializedPoint
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }
    }

    private class SerializedBuildZone
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("sizeX")]
        public float SizeX { get; set; }

        [JsonPropertyName("sizeY")]
        public float SizeY { get; set; }
    }

    private class WaveSet
    {
        [JsonPropertyName("mapId")]
        public string MapId { get; set; }

        [JsonPropertyName("waves")]
        public List<SerializedWave> Waves { get; set; } = new();
    }

    private class SerializedWave
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("spawns")]
        public List<SerializedEnemySpawn> Spawns { get; set; } = new();
    }

    private class SerializedEnemySpawn
    {
        [JsonPropertyName("enemyTypeId")]
        public string EnemyTypeId { get; set; }

        [JsonPropertyName("spawnPointId")]
        public string SpawnPointId { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }
}
