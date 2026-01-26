using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EditorEngine.Enemies;

/// <summary>
/// Реестр конфигураций врагов - загружает JSON файлы из EditorEngine/Enemies/Configs
/// </summary>
public class EnemyConfigRegistry
{
    private static EnemyConfigRegistry _instance;
    public static EnemyConfigRegistry Instance => _instance ??= new EnemyConfigRegistry();

    private Dictionary<string, EnemyConfig> configs = new();

    private EnemyConfigRegistry()
    {
        LoadConfigs();
    }

    private string _activeConfigDir;

    private void LoadConfigs()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] possiblePaths = new[]
        {
            Path.Combine("Content", "Enemies"),
            Path.Combine("Weezer-Tower-Defence-Editor", "Content", "Enemies"),
            Path.Combine("EditorEngine", "Enemies", "Configs"),
            Path.Combine("Weezer-Tower-Defence-Editor", "EditorEngine", "Enemies", "Configs"),
            Path.Combine(baseDir, "Content", "Enemies"),
            Path.Combine("..", "..", "..", "EditorEngine", "Enemies", "Configs")
        };

        _activeConfigDir = possiblePaths.FirstOrDefault(Directory.Exists);

        if (_activeConfigDir == null)
        {
            // Попробуем еще один вариант для Rider/Visual Studio
            string projectRoot = Path.Combine(baseDir, "..", "..", "..");
            string studioPath = Path.Combine(projectRoot, "Weezer-Tower-Defence-Editor", "Content", "Enemies");
            if (Directory.Exists(studioPath))
            {
                _activeConfigDir = studioPath;
            }
        }

        if (_activeConfigDir == null)
        {
            _activeConfigDir = Path.Combine("Content", "Enemies"); // fallback
            Directory.CreateDirectory(_activeConfigDir);
            Console.WriteLine($"[DEBUG_LOG] Created enemy configs directory: {Path.GetFullPath(_activeConfigDir)}");
            return;
        }

        Console.WriteLine($"[DEBUG_LOG] Loading enemy configs from: {Path.GetFullPath(_activeConfigDir)}");
        var jsonFiles = Directory.GetFiles(_activeConfigDir, "*.json");
        
        foreach (var filePath in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<EnemyConfig>(json, options);
                
                if (config != null && !string.IsNullOrEmpty(config.Id))
                {
                    configs[config.Id] = config;
                    Console.WriteLine($"[DEBUG_LOG] Loaded enemy config: {config.DisplayName} (ID: {config.Id}, Behavior: {config.BehaviorId})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG_LOG] Failed to load enemy config from {filePath}: {ex.Message}");
            }
        }
    }

    public EnemyConfig GetConfig(string id) => configs.GetValueOrDefault(id);
    public List<EnemyConfig> GetAllConfigs() => configs.Values.ToList();
    public List<string> GetAllConfigIds() => configs.Keys.ToList();
    
    public void SaveConfig(EnemyConfig config)
    {
        if (string.IsNullOrEmpty(_activeConfigDir))
            _activeConfigDir = Path.Combine("Content", "Enemies");

        Directory.CreateDirectory(_activeConfigDir);
        
        string filePath = Path.Combine(_activeConfigDir, $"{config.Id}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(filePath, json);
        
        configs[config.Id] = config;
        Console.WriteLine($"[DEBUG_LOG] Saved enemy config: {config.DisplayName}");
    }
}
