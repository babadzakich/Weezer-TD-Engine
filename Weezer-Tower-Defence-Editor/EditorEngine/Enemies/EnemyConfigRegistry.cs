using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SimulationEngine.Infrastructure;

namespace EditorEngine.Enemies;

/// <summary>
/// Реестр конфигураций врагов из AppData editor storage.
/// </summary>
public class EnemyConfigRegistry
{
    private static EnemyConfigRegistry _instance;
    public static EnemyConfigRegistry Instance => _instance ??= new EnemyConfigRegistry();

    private readonly Dictionary<string, EnemyConfig> configs = new();

    private EnemyConfigRegistry()
    {
        LoadConfigs();
    }

    private string _activeConfigDir;

    private void LoadConfigs()
    {
        _activeConfigDir = PathService.GetEditorEntityDirectory("enemies");
        Directory.CreateDirectory(_activeConfigDir);

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
            _activeConfigDir = PathService.GetEditorEntityDirectory("enemies");

        Directory.CreateDirectory(_activeConfigDir);
        
        string filePath = Path.Combine(_activeConfigDir, $"{config.Id}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(filePath, json);
        
        configs[config.Id] = config;
        Console.WriteLine($"[DEBUG_LOG] Saved enemy config: {config.DisplayName}");
    }
}
