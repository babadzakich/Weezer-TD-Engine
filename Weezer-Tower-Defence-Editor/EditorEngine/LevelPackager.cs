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
    public class TowerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public string SourceFile { get; set; }

        public string AssemblyFile { get; set; }
        public string TypeName { get; set; }
    }

    public class DamageDealerConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SourceFile { get; set; }

        public string AssemblyFile { get; set; }  // "Plugins/DamageDealers/standard.dll"
        public string TypeName { get; set; }      // "MyPlugins.DamageDealers.StandardBulletBehavior"
    }


    private static void BuildPluginDllFromSingleSource(
        string sourceCsPath,
        string outputDllPath,
        string assemblyName,
        string targetFramework = "net8.0")
    {
        // 1) temp dir
        var tempDir = IOPath.Combine(IOPath.GetTempPath(), "td_plugin_build_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        // 2) copy source
        var csFileName = IOPath.GetFileName(sourceCsPath);
        var tempCsPath = IOPath.Combine(tempDir, csFileName);
        File.Copy(sourceCsPath, tempCsPath, true);

        // 3) reference SimulationEngine assembly
        var simAsmPath = typeof(SimulationEngine.TowerRelated.ITowerBehavior).Assembly.Location;
        // (даже если это bullet-плагин — неважно, это одна и та же сборка SimulationEngine)
        
        // 4) generate csproj
        var csprojPath = IOPath.Combine(tempDir, "Plugin.csproj");
        File.WriteAllText(csprojPath, $@"
    <Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>{targetFramework}</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <AssemblyName>{assemblyName}</AssemblyName>
    </PropertyGroup>

    <ItemGroup>
        <Reference Include=""SimulationEngine"">
        <HintPath>{System.Security.SecurityElement.Escape(simAsmPath)}</HintPath>
        </Reference>
    </ItemGroup>
    </Project>
    ".Trim());

        // 5) build
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build Plugin.csproj -c Release",
            WorkingDirectory = tempDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = System.Diagnostics.Process.Start(psi);
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            throw new Exception(
                $"Plugin build failed for {sourceCsPath}\n" +
                $"STDOUT:\n{stdout}\n\nSTDERR:\n{stderr}");
        }

        // 6) find output dll
        var dllCandidates = Directory.GetFiles(
            IOPath.Combine(tempDir, "bin", "Release"),
            $"{assemblyName}.dll",
            SearchOption.AllDirectories);

        if (dllCandidates.Length == 0)
            throw new Exception($"Built DLL not found for {assemblyName}. Build output:\n{stdout}\n{stderr}");

        Directory.CreateDirectory(IOPath.GetDirectoryName(outputDllPath)!);
        File.Copy(dllCandidates[0], outputDllPath, true);

        // 7) cleanup
        try { Directory.Delete(tempDir, true); } catch { /* ignore */ }
    }

    /// <summary>
    /// Упаковать уровень в архив
    /// </summary>
    public static void PackLevel(GameMap map, WaveSet waveSet, string outputPath)
    {
        // Создаём папку для уровня
        string levelDir = IOPath.Combine("Content", "Levels", map.Id);

        if (Directory.Exists(levelDir))
            Directory.Delete(levelDir, true);

        Directory.CreateDirectory(levelDir);

        try
        {
            // 1. Карта
            string mapJson = IOPath.Combine(levelDir, $"{map.Id}.json");
            MapSerializer.SaveMap(map, mapJson);

            // 2. Волны
            string wavesJson = IOPath.Combine(levelDir, $"{map.Id}.waves.json");
            WaveSerializer.Save(waveSet, wavesJson);

            // 3. Enemies (оставляем как есть)
            string enemiesDir = IOPath.Combine(levelDir, "Enemies");
            Directory.CreateDirectory(enemiesDir);

            var usedEnemyTypes = GetUsedEnemyTypes(waveSet);

            foreach (var enemyTypeId in usedEnemyTypes)
            {
                var enemyConfig = EnemyConfigRegistry.Instance.GetConfig(enemyTypeId);
                if (enemyConfig == null)
                    continue;

                string configPath = IOPath.Combine(enemiesDir, $"{enemyConfig.Id}.json");
                File.WriteAllText(
                    configPath,
                    JsonSerializer.Serialize(enemyConfig, new JsonSerializerOptions { WriteIndented = true })
                );

                var behavior = EnemyBehaviorRegistry.Instance.GetBehavior(enemyConfig.BehaviorId);
                if (behavior == null)
                    continue;

                string behaviorTypeName = behavior.GetType().Name;
                string sourcePath = IOPath.Combine(
                    "EditorEngine", "Enemies", "Behaviors", $"{behaviorTypeName}.cs"
                );

                if (File.Exists(sourcePath))
                {
                    File.Copy(
                        sourcePath,
                        IOPath.Combine(enemiesDir, $"{behaviorTypeName}.cs"),
                        true
                    );
                }
            }

            // 4. Plugins
            string pluginsDir = IOPath.Combine(levelDir, "Plugins");
            string towerPluginsDir = IOPath.Combine(pluginsDir, "Towers");
            string ddPluginsDir = IOPath.Combine(pluginsDir, "DamageDealers");

            Directory.CreateDirectory(towerPluginsDir);
            Directory.CreateDirectory(ddPluginsDir);

            // 5. Towers
            string towersDir = IOPath.Combine(levelDir, "Towers");
            Directory.CreateDirectory(towersDir);

            var allTowerTypes = TowerTypeRegistry.Instance.GetAllTowerTypes();

            foreach (var towerInfo in allTowerTypes)
            {
                string src = IOPath.Combine("PluginSources", "Towers", $"{towerInfo.Id}.cs");
                string dllOut = IOPath.Combine(towerPluginsDir, $"{towerInfo.Id}.dll");

                BuildPluginDll(src, dllOut, $"tower_{towerInfo.Id}");

                var cfg = new TowerConfig
                {
                    Id = towerInfo.Id,
                    Name = towerInfo.Name,
                    Cost = towerInfo.Cost,
                    Range = towerInfo.Range,
                    FireRate = towerInfo.FireRate,
                    AssemblyFile = $"Plugins/Towers/{towerInfo.Id}.dll",
                    TypeName = $"MyPlugins.Towers.{ToPascal(towerInfo.Id)}Behavior"
                };

                File.WriteAllText(
                    IOPath.Combine(towersDir, $"{towerInfo.Id}.json"),
                    JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true })
                );
            }

            // 6. DamageDealers
            string damageDealersDir = IOPath.Combine(levelDir, "DamageDealers");
            Directory.CreateDirectory(damageDealersDir);

            var allDamageDealerTypes = DamageDealerTypeRegistry.Instance.GetAllDamageDealerTypes();

            foreach (var ddInfo in allDamageDealerTypes)
            {
                string src = IOPath.Combine("PluginSources", "DamageDealers", $"{ddInfo.Id}.cs");
                string dllOut = IOPath.Combine(ddPluginsDir, $"{ddInfo.Id}.dll");

                BuildPluginDll(src, dllOut, $"dd_{ddInfo.Id}");

                var cfg = new DamageDealerConfig
                {
                    Id = ddInfo.Id,
                    Name = ddInfo.Name,
                    AssemblyFile = $"Plugins/DamageDealers/{ddInfo.Id}.dll",
                    TypeName = $"MyPlugins.DamageDealers.{ToPascal(ddInfo.Id)}Behavior"
                };

                File.WriteAllText(
                    IOPath.Combine(damageDealersDir, $"{ddInfo.Id}.json"),
                    JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true })
                );
            }

            // 7. README
            string readme = IOPath.Combine(levelDir, "README.txt");
            File.WriteAllText(readme, GenerateReadme(map, waveSet, usedEnemyTypes));

            // 8. Архив
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            ZipFile.CreateFromDirectory(levelDir, outputPath);
        }
        catch
        {
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
