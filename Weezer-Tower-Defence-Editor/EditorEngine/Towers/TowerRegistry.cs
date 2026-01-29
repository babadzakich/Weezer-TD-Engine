using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SimulationEngine.Persistence;


public class TowerRegistry
{
    private static TowerRegistry _instance;
    public static TowerRegistry Instance => _instance ??= new TowerRegistry();
    private TowerRegistry()
    {
        loadTowersClasses();
        loadTowersConfigs();
    }

    public void Update()
    {
        towers = new();
        loadTowersConfigs();
    }


    public Dictionary<string, BehaviorConfig> behaviorDescriptions = new();
    public Dictionary<string, Type> behaviors = new();
    public Dictionary<string, List<TypeSpecification>> towers = new();



    /// <summary>
    /// This method loads damage dealer behavior as classes from dll. 
    /// It uses json files to find out which dll to load and which class to look for
    /// Also json-s describe constructor arguments
    /// </summary>
    private void loadTowersClasses()
    {

        var jsonRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom",
            "towers",
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
                "towers",
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

    private void loadTowersConfigs()
    {
        var result = new Dictionary<string, List<TypeSpecification>>();

        var jsonRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom",
            "towers",
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

            var spec = JsonSerializer.Deserialize<List<TypeSpecification>>(json, jsonOptions);
            if (spec == null)
                throw new Exception($"Failed to parse {jsonPath}");
            if (spec.Count == 0)
                throw new Exception($"No tower levels in {jsonPath}");

            var className = spec[0].ClassName;

            if (!behaviors.ContainsKey(className))
                throw new Exception(
                    $"Behavior class '{className}' not found (config: {jsonPath})"
                );

            var name = spec[0].Name;
            if (result.ContainsKey(name))
                throw new Exception($"Duplicate damage dealer config: {name}");

            result[name] = spec;
        }

        towers = result;
    }
}

