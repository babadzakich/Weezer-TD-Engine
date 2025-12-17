using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using SimulationEngine.MapRelated;
using EditorEngine.Waves;
using EditorEngine.Enemies;
using EditorEngine.Towers;
using EditorEngine.DamageDealers;
using IOPath = System.IO.Path;

namespace EditorEngine;

/// <summary>
/// Упаковщик уровня - создаёт архив со всем содержимым уровня
/// </summary>
public static class LevelPackager
{
    public class EnemyConfig
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public int Health { get; set; }
        public float Speed { get; set; }
        public int Damage { get; set; }
        public string SourceFile { get; set; } // Имя .cs файла
    }

    public class TowerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public string SourceFile { get; set; }
    }

    public class DamageDealerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SourceFile { get; set; }
    }

    /// <summary>
    /// Упаковать уровень в архив
    /// </summary>
    public static void PackLevel(GameMap map, WaveSet waveSet, string outputPath)
    {
        // Создаём папку для уровня в Content/Levels/
        string levelDir = IOPath.Combine("Content", "Levels", map.Id);
        
        // Если папка существует, очищаем её
        if (Directory.Exists(levelDir))
            Directory.Delete(levelDir, true);
            
        Directory.CreateDirectory(levelDir);

        try
        {
            // Создаём структуру папок
            string mapsDir = IOPath.Combine(levelDir, "Maps");
            Directory.CreateDirectory(mapsDir);

            // 1. Сохраняем карту в папку Maps
            string mapJson = IOPath.Combine(mapsDir, $"{map.Id}.json");
            MapSerializer.SaveMap(map, mapJson);

            // 2. Сохраняем волны в папку Maps
            string wavesJson = IOPath.Combine(mapsDir, $"{map.Id}.waves.json");
            WaveSerializer.Save(waveSet, wavesJson);

            // 3. Создаём папку для врагов внутри Maps
            string enemiesDir = IOPath.Combine(mapsDir, "Enemies");
            Directory.CreateDirectory(enemiesDir);
            Console.WriteLine($"Created Enemies directory: {enemiesDir}");

            // Получаем все типы врагов, используемые в волнах
            var usedEnemyTypes = GetUsedEnemyTypes(waveSet);
            Console.WriteLine($"Found {usedEnemyTypes.Count} enemy types in waves");
            
            foreach (var enemyTypeId in usedEnemyTypes)
            {
                var enemyInfo = EnemyTypeRegistry.Instance.GetEnemyInfo(enemyTypeId);
                if (enemyInfo != null)
                {
                    // Сохраняем конфиг врага
                    var config = new EnemyConfig
                    {
                        Id = enemyInfo.Id,
                        DisplayName = enemyInfo.DisplayName,
                        Health = enemyInfo.BaseHealth,
                        Speed = enemyInfo.BaseSpeed,
                        Damage = enemyInfo.Damage,
                        SourceFile = $"{enemyInfo.Type.Name}.cs"
                    };

                    string configPath = IOPath.Combine(enemiesDir, $"{enemyInfo.Id}.json");
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(configPath, JsonSerializer.Serialize(config, options));

                    // Копируем исходный .cs файл
                    // Пытаемся найти файл в разных местах
                    string[] possiblePaths = new[]
                    {
                        // В исходниках проекта
                        IOPath.Combine(Directory.GetCurrentDirectory(), "EditorEngine", "Enemies", "Types", $"{enemyInfo.Type.Name}.cs"),
                        // Рядом с exe
                        IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "EditorEngine", "Enemies", "Types", $"{enemyInfo.Type.Name}.cs"),
                        // Относительно рабочей директории
                        IOPath.Combine("EditorEngine", "Enemies", "Types", $"{enemyInfo.Type.Name}.cs")
                    };

                    string sourceFile = null;
                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            sourceFile = path;
                            break;
                        }
                    }

                    if (sourceFile != null)
                    {
                        string destFile = IOPath.Combine(enemiesDir, $"{enemyInfo.Type.Name}.cs");
                        File.Copy(sourceFile, destFile, true);
                        Console.WriteLine($"Copied enemy source: {enemyInfo.Type.Name}.cs");
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Source file not found for {enemyInfo.Type.Name}");
                    }
                }
            }

            // Если в папке Enemies ничего нет, создаём файл-заглушку
            // (пустые папки не попадают в zip-архив)
            if (Directory.GetFiles(enemiesDir).Length == 0)
            {
                string placeholder = IOPath.Combine(enemiesDir, ".gitkeep");
                File.WriteAllText(placeholder, "This folder will contain enemy configurations and source files.");
                Console.WriteLine("Created placeholder in empty Enemies folder");
            }

            // 4. Создаём папку для башен внутри Maps
            string towersDir = IOPath.Combine(mapsDir, "Towers");
            Directory.CreateDirectory(towersDir);
            Console.WriteLine($"Created Towers directory: {towersDir}");

            // Получаем все типы башен из TowerTypeRegistry
            var allTowerTypes = TowerTypeRegistry.Instance.GetAllTowerTypes();
            Console.WriteLine($"Found {allTowerTypes.Count} tower types");

            foreach (var towerInfo in allTowerTypes)
            {
                // Сохраняем конфиг башни
                var config = new TowerConfig
                {
                    Id = towerInfo.Id,
                    Name = towerInfo.Name,
                    Cost = towerInfo.Cost,
                    Range = towerInfo.Range,
                    FireRate = towerInfo.FireRate,
                    SourceFile = $"{towerInfo.Type.Name}.cs"
                };

                string configPath = IOPath.Combine(towersDir, $"{towerInfo.Id}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(configPath, JsonSerializer.Serialize(config, options));

                // Копируем исходный .cs файл башни
                CopySourceFile(towerInfo.Type, towersDir, "EditorEngine", "Towers", "Types");
            }

            // Если папка Towers пустая, создаём заглушку
            if (Directory.GetFiles(towersDir).Length == 0)
            {
                string placeholder = IOPath.Combine(towersDir, ".gitkeep");
                File.WriteAllText(placeholder, "This folder will contain tower configurations and source files.");
                Console.WriteLine("Created placeholder in empty Towers folder");
            }

            // 5. Создаём папку для damage dealers внутри Maps
            string damageDealersDir = IOPath.Combine(mapsDir, "DamageDealers");
            Directory.CreateDirectory(damageDealersDir);
            Console.WriteLine($"Created DamageDealers directory: {damageDealersDir}");

            // Получаем все типы damage dealers
            var allDamageDealerTypes = DamageDealerTypeRegistry.Instance.GetAllDamageDealerTypes();
            Console.WriteLine($"Found {allDamageDealerTypes.Count} damage dealer types");

            foreach (var ddInfo in allDamageDealerTypes)
            {
                // Сохраняем конфиг
                var config = new DamageDealerConfig
                {
                    Id = ddInfo.Id,
                    Name = ddInfo.Name,
                    SourceFile = $"{ddInfo.Type.Name}.cs"
                };

                string configPath = IOPath.Combine(damageDealersDir, $"{config.Id}.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(configPath, JsonSerializer.Serialize(config, options));

                // Копируем исходный .cs файл
                CopySourceFile(ddInfo.Type, damageDealersDir, "EditorEngine", "DamageDealers", "Types");
            }

            // Если папка DamageDealers пустая, создаём заглушку
            if (Directory.GetFiles(damageDealersDir).Length == 0)
            {
                string placeholder = IOPath.Combine(damageDealersDir, ".gitkeep");
                File.WriteAllText(placeholder, "This folder will contain damage dealer configurations and source files.");
                Console.WriteLine("Created placeholder in empty DamageDealers folder");
            }

            // 6. Создаём README
            string readme = IOPath.Combine(levelDir, "README.txt");
            File.WriteAllText(readme, GenerateReadme(map, waveSet, usedEnemyTypes));

            // 5. Упаковываем всё в архив
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            ZipFile.CreateFromDirectory(levelDir, outputPath);

            Console.WriteLine($"Level directory created: {levelDir}");
            Console.WriteLine($"Level packed successfully: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error packing level: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Распаковать уровень из архива
    /// </summary>
    public static (GameMap map, WaveSet waveSet) UnpackLevel(string archivePath, string extractPath)
    {
        // Создаём папку для распаковки
        if (Directory.Exists(extractPath))
            Directory.Delete(extractPath, true);

        Directory.CreateDirectory(extractPath);

        // Распаковываем архив
        ZipFile.ExtractToDirectory(archivePath, extractPath);

        // Загружаем карту
        string mapPath = IOPath.Combine(extractPath, "map.json");
        GameMap map = MapSerializer.LoadMap(mapPath);

        // Загружаем волны
        string wavesPath = IOPath.Combine(extractPath, "waves.json");
        WaveSet waveSet = WaveSerializer.Load(wavesPath);

        // Копируем файлы врагов в рабочую папку
        string enemiesDir = IOPath.Combine(extractPath, "Enemies");
        if (Directory.Exists(enemiesDir))
        {
            string targetDir = IOPath.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "EditorEngine", "Enemies", "Types"
            );

            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(enemiesDir, "*.cs"))
            {
                string fileName = IOPath.GetFileName(file);
                string targetFile = IOPath.Combine(targetDir, fileName);
                File.Copy(file, targetFile, true);
            }
        }

        return (map, waveSet);
    }

    private static HashSet<string> GetUsedEnemyTypes(WaveSet waveSet)
    {
        var types = new HashSet<string>();
        
        foreach (var wave in waveSet.Waves)
        {
            foreach (var spawn in wave.Spawns)
            {
                types.Add(spawn.EnemyTypeId);
            }
        }

        return types;
    }

    private static void CopySourceFile(Type type, string targetDir, params string[] pathSegments)
    {
        string fileName = $"{type.Name}.cs";
        
        // Пытаемся найти файл в разных местах
        var possiblePaths = new List<string>
        {
            // В исходниках проекта
            IOPath.Combine(new[] { Directory.GetCurrentDirectory() }.Concat(pathSegments).Concat(new[] { fileName }).ToArray()),
            // Рядом с exe
            IOPath.Combine(new[] { AppDomain.CurrentDomain.BaseDirectory }.Concat(pathSegments).Concat(new[] { fileName }).ToArray()),
            // Относительно рабочей директории
            IOPath.Combine(pathSegments.Concat(new[] { fileName }).ToArray())
        };

        string sourceFile = null;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                sourceFile = path;
                break;
            }
        }

        if (sourceFile != null)
        {
            string destFile = IOPath.Combine(targetDir, fileName);
            File.Copy(sourceFile, destFile, true);
            Console.WriteLine($"Copied source file: {fileName}");
        }
        else
        {
            Console.WriteLine($"Warning: Source file not found for {type.Name}");
        }
    }

    private static string GenerateReadme(GameMap map, WaveSet waveSet, HashSet<string> enemyTypes)
    {
        return $@"=== LEVEL PACKAGE ===

Level ID: {map.Id}
Level Name: {map.Name}
Map Size: {map.Width} x {map.Height}

STATISTICS:
- Spawn Points: {map.SpawnPoints.Count}
- Defense Points: {map.DefensePoints.Count}
- Paths: {map.Paths.Count}
- Build Zones: {map.BuildZones.Count}
- Waves: {waveSet.Waves.Count}
- Enemy Types: {enemyTypes.Count}

CONTENTS:
Maps/
  - {map.Id}.json           : Map data (spawns, defense, paths, build zones)
  - {map.Id}.waves.json     : Wave configurations
  - Enemies/                : Enemy configurations and source files
  - Towers/                 : Tower configurations and source files
  - DamageDealers/          : Damage dealer configurations and source files

Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";
    }
}
