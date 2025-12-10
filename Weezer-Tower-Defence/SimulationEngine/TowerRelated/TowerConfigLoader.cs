using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SimulationEngine.TowerRelated;

/// <summary>
/// Загрузчик конфигов башен из JSON
/// </summary>
public class TowerConfigLoader
{
    private Dictionary<string, TowerConfig> _configs;

    public TowerConfigLoader()
    {
        _configs = new Dictionary<string, TowerConfig>();
    }

    /// <summary>
    /// Загрузить конфиги из JSON файла
    /// </summary>
    public void LoadFromFile(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<TowerConfigFile>(json);
            
            if (data?.Towers != null)
            {
                foreach (var config in data.Towers)
                {
                    _configs[config.Id] = config;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tower configs from {filePath}: {ex.Message}");
        }
    }

    /// <summary>
    /// Загрузить конфиги из JSON строки
    /// </summary>
    public void LoadFromJson(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TowerConfigFile>(json);
            
            if (data?.Towers != null)
            {
                foreach (var config in data.Towers)
                {
                    _configs[config.Id] = config;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tower configs from JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Добавить конфиг программно (для плагинов)
    /// </summary>
    public void AddConfig(TowerConfig config)
    {
        _configs[config.Id] = config;
    }

    /// <summary>
    /// Получить конфиг по ID
    /// </summary>
    public TowerConfig GetConfig(string id)
    {
        return _configs.TryGetValue(id, out var config) ? config : null;
    }

    /// <summary>
    /// Получить все конфиги
    /// </summary>
    public IEnumerable<TowerConfig> GetAllConfigs()
    {
        return _configs.Values;
    }

    /// <summary>
    /// Проверить, существует ли конфиг
    /// </summary>
    public bool HasConfig(string id)
    {
        return _configs.ContainsKey(id);
    }
}

/// <summary>
/// Структура JSON файла с конфигами башен
/// </summary>
public class TowerConfigFile
{
    public List<TowerConfig> Towers { get; set; }
}
