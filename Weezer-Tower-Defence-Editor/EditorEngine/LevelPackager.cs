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
using System.Net.Http.Headers;

namespace EditorEngine;

/// <summary>
/// Упаковщик уровня - создаёт архив со всем содержимым уровня
/// </summary>
public static class LevelPackager
{

    /// <summary>
    /// Упаковать уровень в архив
    /// </summary>
    public static void PackLevel(GameMap map, WaveSet waveSet, string outputPath)
    {
        // Создаём папку для уровня в $APP_DATA$/WeezerTowerDefence/Levels/{map.Id}
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var levelDir = System.IO.Path.Combine(
            appData,
            "WeezerTowerDefence",
            "Levels",
            $"{map.Id}"
        );

        // Если папка существует, очищаем её
        if (Directory.Exists(levelDir))
            Directory.Delete(levelDir, true);
            
        Directory.CreateDirectory(levelDir);

        try
        {

            // 1. Сохраняем карту
            string mapJson = IOPath.Combine(levelDir, $"{map.Id}.json");
            MapSerializer.SaveMap(map, mapJson);

            // 2. Сохраняем волны
            string wavesJson = IOPath.Combine(levelDir, $"{map.Id}.waves.json");
            WaveSerializer.Save(waveSet, wavesJson);

            // 3. Сохраняем всех врагов. Господь узнает своих.
            string originalEnemiesDir = System.IO.Path.Combine(
                appData,
                "WeezerTowerDefence",
                "Editor",
                "custom",
                "enemies"
            );

            string enemiesDir = IOPath.Combine(levelDir, "Enemies");

            hardCopyJson(originalEnemiesDir, enemiesDir);

            // 4. Создаём папку для башен
            string originalTowersDir = System.IO.Path.Combine(
                appData,
                "WeezerTowerDefence",
                "Editor",
                "custom",
                "towers"
            );

            string towersDir = IOPath.Combine(levelDir, "Towers");

            hardCopy(originalTowersDir, towersDir);
            

            // 5. Создаём папку для damage dealers
            string originalDamageDealersDir = System.IO.Path.Combine(
                 appData,
                 "WeezerTowerDefence",
                 "Editor",
                 "custom",
                 "damageDealers"
             );

            string damageDealersDir = IOPath.Combine(levelDir, "DamageDealers");

            hardCopy(originalDamageDealersDir, damageDealersDir);

            // 7. Копируем MoneyHealth.json если он есть
            saveMoneyHealth(levelDir);

            // 8. Копируем все DLL-ки
            string originalDLLs = System.IO.Path.Combine(
                 appData,
                 "WeezerTowerDefence",
                 "DLLs"
             );

            string dllsDir = IOPath.Combine(levelDir, "DLLs");

            hardCopy(originalDLLs, dllsDir);

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


    static private void saveMoneyHealth(string exitPath) 
    {
        if (!File.Exists(IOPath.Combine("Content", "MoneyHealth.json")))
        {
            string configPath = System.IO.Path.Combine("Content", "MoneyHealth.json");

            var config = new
            {
                StartingMoney = 500,
                StartingLives = 100
            };

            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string json = System.Text.Json.JsonSerializer.Serialize(config, options);
            System.IO.File.WriteAllText(configPath, json);
        }

        File.Copy(IOPath.Combine("Content", "MoneyHealth.json"), IOPath.Combine(exitPath, "MoneyHealth.json"), true);
    }


    private static void hardCopy(string source, string dest)
    {
        if (Directory.Exists(dest))
        {
            Directory.Delete(dest, recursive: true);
        }

        Directory.CreateDirectory(dest);

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relativePath = IOPath.GetRelativePath(source, file);
            var targetPath = IOPath.Combine(dest, relativePath);

            var targetDir = IOPath.GetDirectoryName(targetPath)!;
            Directory.CreateDirectory(targetDir);

            File.Copy(file, targetPath);
        }
    }

    private static void hardCopyJson(string source, string dest)
    {
        if (Directory.Exists(dest))
        {
            Directory.Delete(dest, recursive: true);
        }

        Directory.CreateDirectory(dest);

        foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relativeDir = IOPath.GetRelativePath(source, dir);
            var targetDir = IOPath.Combine(dest, relativeDir);
            Directory.CreateDirectory(targetDir);
        }

        foreach (var file in Directory.GetFiles(source, "*.json", SearchOption.AllDirectories))
        {
            var relativePath = IOPath.GetRelativePath(source, file);
            var targetPath = IOPath.Combine(dest, relativePath);

            File.Copy(file, targetPath, overwrite: true);
        }
    }

}
