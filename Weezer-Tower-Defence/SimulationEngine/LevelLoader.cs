using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.MapRelated;
using SimulationEngine.WaveRelated;
using GamePath = SimulationEngine.MapRelated.Path;
using IOPath = System.IO.Path;

namespace SimulationEngine;

/// <summary>
/// Загрузчик уровня из упакованного архива
/// </summary>
public class LevelLoader
{
    public class LoadedLevel
    {
        public GameMap Map { get; set; }
        public List<WaveData> Waves { get; set; }
        public Dictionary<string, EnemyDefinition> EnemyDefinitions { get; set; }
        public Dictionary<string, TowerDefinition> TowerDefinitions { get; set; }
        public Dictionary<string, DamageDealerDefinition> DamageDealerDefinitions { get; set; }
        public MoneyHealthSettings MoneyHealthSettings { get; set; }
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
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        [JsonPropertyName("behaviorId")]
        public string BehaviorId { get; set; }
        [JsonPropertyName("baseHealth")]
        public int BaseHealth { get; set; }
        [JsonPropertyName("baseSpeed")]
        public float BaseSpeed { get; set; }
        [JsonPropertyName("damage")]
        public int Damage { get; set; }
    }

    public class TowerDefinition
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string BulletClassName { get; set; }
        [JsonPropertyName("cost")]
        public int Cost { get; set; }
        [JsonPropertyName("range")]
        public float Range { get; set; }
        [JsonPropertyName("fireRate")]
        public float FireRate { get; set; }
        [JsonPropertyName("damage")]
        public float Damage { get; set; }
        
        // Editor saves as "Upgrades", old levels might use "upgradeLevels"
        [JsonPropertyName("upgrades")]
        public List<TowerUpgradeDefinition> Upgrades { get; set; } = new();

        [JsonPropertyName("upgradeLevels")]
        public List<TowerUpgradeDefinition> UpgradeLevels 
        { 
            get => Upgrades; 
            set => Upgrades = value; 
        }
    }

    public class TowerUpgradeDefinition
    {
        [JsonPropertyName("cost")]
        public int Cost { get; set; }

        [JsonPropertyName("upgradeCost")]
        public int UpgradeCost 
        { 
            get => Cost; 
            set => Cost = value; 
        }

        [JsonPropertyName("range")]
        public float Range { get; set; }
        [JsonPropertyName("fireRate")]
        public float FireRate { get; set; }
        [JsonPropertyName("damage")]
        public float Damage { get; set; }
    }

    public class DamageDealerDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class MoneyHealthSettings
    {
        public int StartingMoney { get; set; }
        public int StartingLives { get; set; }
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
                .FirstOrDefault(f => !f.Contains(".waves.") && !f.Contains("Money"));
            
            if (mapFile == null)
                throw new Exception("Map file not found in archive");

            level.Map = LoadMap(mapFile);
            Console.WriteLine($"Loaded map: {level.Map.Name}");

            // 2. Загружаем волны
            string wavesFile = IOPath.Combine(tempDir, $"{level.Map.Id}.waves.json");
            if (!File.Exists(wavesFile))
            {
                // Попытка найти любой файл .waves.json если именной не найден
                wavesFile = Directory.GetFiles(tempDir, "*.waves.json", SearchOption.TopDirectoryOnly).FirstOrDefault();
            }

            if (wavesFile != null && File.Exists(wavesFile))
            {
                level.Waves = LoadWaves(wavesFile);
                Console.WriteLine($"Loaded {level.Waves.Count} waves from {IOPath.GetFileName(wavesFile)}");
            }
            else
            {
                level.Waves = new List<WaveData>();
                Console.WriteLine("No waves file found");
            }

            // 6. Загружаем настройки денег и здоровья
            string moneyHealthDir = IOPath.Combine(tempDir, "MoneyHealth.json");
            if (File.Exists(moneyHealthDir))
            {
                level.MoneyHealthSettings = LoadMoneyHealthSettings(moneyHealthDir);
                Console.WriteLine($"Loaded Money: {level.MoneyHealthSettings.StartingMoney}; lives: {level.MoneyHealthSettings.StartingLives}");
            }
            else
            {
                // Дефолтные значения если файл отсутствует
                level.MoneyHealthSettings = new MoneyHealthSettings
                {
                    StartingMoney = 100,
                    StartingLives = 20
                };
                Console.WriteLine("MoneyHealth.json not found, using defaults (100 money, 20 lives)");
            }



            EnemyRegistry.ResetEnemies(
                IOPath.Combine(tempDir, "Dlls", "enemies"),
                IOPath.Combine(tempDir, "enemies", "configs"),
                IOPath.Combine(tempDir, "enemies", "behaviors")
                );

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
        
        var options = new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        };
        var serializedMap = JsonSerializer.Deserialize<SerializedMap>(json, options);

        var map = new GameMap(serializedMap.Id, serializedMap.Name, serializedMap.Width, serializedMap.Height);

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
            if (points.Count == 0) continue;

            // Пытаемся привязать путь к точке спавна по координатам
            var spawnPoint = map.SpawnPoints.FirstOrDefault(sp => Microsoft.Xna.Framework.Vector2.Distance(sp.Position, points[0]) < 1f);
            if (spawnPoint == null) spawnPoint = map.SpawnPoints.FirstOrDefault(); // fallback
            
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
                Console.WriteLine($"Linked SpawnPoint {spawnPoint.Id} to Path {path.Id} (dist: {Microsoft.Xna.Framework.Vector2.Distance(spawnPoint.Position, points[0])})");
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var waveSet = JsonSerializer.Deserialize<WaveSet>(json, options);
        
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

    private static void LinkDLLs(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            Console.WriteLine($"Folder not found: {rootPath}");
            return;
        }

        foreach (var dllPath in Directory.EnumerateFiles(
                     rootPath, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                Console.WriteLine($"Loading: {dllPath}");
                var assembly = Assembly.LoadFrom(dllPath);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {dllPath}: {ex.Message}");
            }
        }
    }

    private static void LoadEnemyDefinitions(string enemiesDir, Dictionary<string, EnemyDefinition> definitions)
    {
        var jsonFiles = Directory.GetFiles(enemiesDir, "*.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        foreach (var file in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<EnemyDefinition>(json, options);
                
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        foreach (var file in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<TowerDefinition>(json, options);
                
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
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        
        foreach (var file in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                var def = JsonSerializer.Deserialize<DamageDealerDefinition>(json, options);
                
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

    // Tries to find config. Returns default if not found or failed to load.
    private static MoneyHealthSettings LoadMoneyHealthSettings(string mhDir)
    {
        var settings = new MoneyHealthSettings
        {
            StartingMoney = 100,
            StartingLives = 20
        };

        string json = File.ReadAllText(mhDir);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        settings = JsonSerializer.Deserialize<MoneyHealthSettings>(json, options);
        return settings;
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
