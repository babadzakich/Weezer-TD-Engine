using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SimulationEngine.Persistence;


public class EnemyRegistry
{
    private static EnemyRegistry _instance;
    public static EnemyRegistry Instance => _instance ??= new EnemyRegistry();
    private EnemyRegistry()
    {
        loadClasses();
        loadConfigs();
    }

    public void Update()
    {
        enemies = new();
        loadConfigs();
    }

    public Dictionary<string, BehaviorConfig> behaviorDescriptions = new();
    public Dictionary<string, Type> behaviors = new();
    public Dictionary<string, TypeSpecification> enemies = new();



    /// <summary>
    /// This method loads damage dealer behavior as classes from dll. 
    /// It uses json files to find out which dll to load and which class to look for
    /// Also json-s describe constructor arguments
    /// </summary>
    private void loadClasses()
    {

        var jsonRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom",
            "enemies",
            "behaviors"
        );
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (!Directory.Exists(jsonRoot))
            throw new DirectoryNotFoundException(jsonRoot);

        foreach (var jsonPath in Directory.EnumerateFiles(jsonRoot, "*.json"))
        {
            var json = File.ReadAllText(jsonPath);
            Console.WriteLine($"Loading behavior from {jsonPath}");

            var config = JsonSerializer.Deserialize<BehaviorConfig>(json, jsonOptions);
            if (config == null)
                throw new Exception($"Failed to parse {jsonPath}");

            if (behaviorDescriptions.ContainsKey(config.ClassName))
                throw new Exception($"Duplicate behavior name: {config.ClassName}");

            behaviorDescriptions[config.ClassName] = config;

            var dllPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WeezerTowerDefence",
                "DLLs",
                "enemies",
                $"{config.FileName}.dll"
             );

            if (!File.Exists(dllPath))
                throw new FileNotFoundException(dllPath);

            var assembly = Assembly.LoadFrom(dllPath);
            var type = assembly
                .GetTypes()
                .FirstOrDefault(t => t.Name == config.ClassName);
            if (type == null)
                throw new Exception(
                    $"Type {config.ClassName} not found in {dllPath}"
                );
            if (behaviors.ContainsKey(config.ClassName))
                throw new Exception($"Duplicate behavior class: {config.ClassName}");

            behaviors[config.ClassName] = type;
        }
    }

    private void loadConfigs()
    {
        var result = new Dictionary<string, TypeSpecification>();

        var jsonRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom",
            "enemies",
            "configs"
        );

        if (!Directory.Exists(jsonRoot))
            throw new DirectoryNotFoundException(jsonRoot);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var jsonPath in Directory.EnumerateFiles(jsonRoot, "*.json"))
        {
            var json = File.ReadAllText(jsonPath);

            var spec = JsonSerializer.Deserialize<TypeSpecification>(json, jsonOptions);
            if (spec == null)
                throw new Exception($"Failed to parse {jsonPath}");

            if (!behaviors.ContainsKey(spec.ClassName))
                throw new Exception(
                    $"Behavior class '{spec.ClassName}' not found (config: {jsonPath})"
                );

            if (result.ContainsKey(spec.Name))
                throw new Exception($"Duplicate damage dealer config: {spec.Name}");

            result[spec.Name] = spec;
        }

        enemies = result;
    }
}
