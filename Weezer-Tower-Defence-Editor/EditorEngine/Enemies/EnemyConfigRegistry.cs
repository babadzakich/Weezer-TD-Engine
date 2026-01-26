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

    private void LoadConfigs()
    {
        string configDir = Path.Combine("EditorEngine", "Enemies", "Configs");
        
        if (!Directory.Exists(configDir))
        {
            Directory.CreateDirectory(configDir);
            Console.WriteLine($"Created enemy configs directory: {configDir}");
            return;
        }

        var jsonFiles = Directory.GetFiles(configDir, "*.json");
        
        foreach (var filePath in jsonFiles)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<EnemyConfig>(json);
                
                if (config != null && !string.IsNullOrEmpty(config.Id))
                {
                    configs[config.Id] = config;
                    Console.WriteLine($"Loaded enemy config: {config.DisplayName} (ID: {config.Id}, Behavior: {config.BehaviorId})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load enemy config from {filePath}: {ex.Message}");
            }
        }
    }

    public EnemyConfig GetConfig(string id) => configs.GetValueOrDefault(id);
    public List<EnemyConfig> GetAllConfigs() => configs.Values.ToList();
    public List<string> GetAllConfigIds() => configs.Keys.ToList();
    
    public void SaveConfig(EnemyConfig config)
    {
        string configDir = Path.Combine("EditorEngine", "Enemies", "Configs");
        Directory.CreateDirectory(configDir);
        
        string filePath = Path.Combine(configDir, $"{config.Id}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(filePath, json);
        
        configs[config.Id] = config;
        Console.WriteLine($"Saved enemy config: {config.DisplayName}");
    }
}
