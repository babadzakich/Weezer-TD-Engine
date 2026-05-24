using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SimulationEngine.Infrastructure;


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
        behaviorDescriptions = new();
        loadClasses();
        loadConfigs();
    }

    public Dictionary<string, BehaviorConfig> behaviorDescriptions = new();
    public Dictionary<string, Type> behaviors = new();
    public Dictionary<string, TypeSpecification> enemies = new();

    private void loadClasses()
    {
        var jsonRoot = Path.Combine(
            PathService.EditorDirectory,
            "enemies",
            "behaviors"
        );
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        if (!Directory.Exists(jsonRoot))
            return;

        foreach (var jsonPath in Directory.EnumerateFiles(jsonRoot, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(jsonPath);
                var config = JsonSerializer.Deserialize<BehaviorConfig>(json, jsonOptions);
                if (config == null) continue;

                behaviorDescriptions[config.ClassName] = config;

                var dllPath = Path.Combine(
                    PathService.DLLsDirectory,
                    "enemies",
                    $"{config.FileName}.dll"
                );

                if (File.Exists(dllPath))
                {
                    var dllBytes = File.ReadAllBytes(dllPath);
                    var assembly = Assembly.Load(dllBytes);
                    var type = assembly.GetTypes().FirstOrDefault(t => t.Name == config.ClassName);
                    if (type != null)
                    {
                        behaviors[config.ClassName] = type;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to load behavior from {jsonPath}: {ex.Message}");
            }
        }

        // Добавляем встроенные поведения из движка
        AddBuiltInBehaviors();
    }

    private void AddBuiltInBehaviors()
    {
        // Базовый враг
        if (!behaviorDescriptions.ContainsKey("BasicEnemyType"))
        {
            var basicConfig = new BehaviorConfig
            {
                Name = "Basic Enemy",
                ClassName = "BasicEnemyType",
                FileName = "internal",
                Args = new List<ArgConfig>
                {
                    new ArgConfig { Name = "health", Type = "int" },
                    new ArgConfig { Name = "speed", Type = "float" },
                    new ArgConfig { Name = "damage", Type = "int" },
                    new ArgConfig { Name = "hitRadius", Type = "float" }
                }
            };
            behaviorDescriptions[basicConfig.ClassName] = basicConfig;
        }
    }

    private void loadConfigs()
    {
        var result = new Dictionary<string, TypeSpecification>();

        var jsonRoot = Path.Combine(
            PathService.EditorDirectory,
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

            // if (!behaviors.ContainsKey(spec.ClassName))
            //     throw new Exception(
            //         $"Behavior class '{spec.ClassName}' not found (config: {jsonPath})"
            //     );

            if (result.ContainsKey(spec.Name))
                throw new Exception($"Duplicate damage dealer config: {spec.Name}");

            result[spec.Name] = spec;
        }

        enemies = result;
    }
}
