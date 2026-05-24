using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SimulationEngine.Infrastructure;
using SimulationEngine.Persistence;


public class TowerRegistry
{
    private static TowerRegistry _instance;
    public static TowerRegistry Instance => _instance ??= new TowerRegistry();
    private TowerRegistry()
    {
        // loadTowersClasses();
        loadTowersConfigs();
    }

    public void Update()
    {
        towers = new();
        loadTowersConfigs();
    }


    public Dictionary<string, BehaviorConfig> behaviorDescriptions = [];

    public Dictionary<string, List<TypeSpecification>> towers = [];



    /// <summary>
    /// This method loads damage dealer behavior as classes from dll. 
    /// It uses json files to find out which dll to load and which class to look for
    /// Also json-s describe constructor arguments
    /// </summary>
    // private void loadTowersClasses()
    // {

    //     var jsonRoot = System.IO.Path.Combine(
    //         PathService.EditorDirectory,
    //         "towers",
    //         "behaviors"
    //     );
    //     var jsonOptions = new JsonSerializerOptions
    //     {
    //         PropertyNameCaseInsensitive = true
    //     };

    //     if (!Directory.Exists(jsonRoot))
    //         throw new DirectoryNotFoundException(jsonRoot);

    //     foreach (var jsonPath in Directory.EnumerateFiles(jsonRoot, "*.json"))
    //     {
    //         var json = File.ReadAllText(jsonPath);
    //         Console.WriteLine($"Loading behavior from {jsonPath}");

    //         var config = JsonSerializer.Deserialize<BehaviorConfig>(json, jsonOptions);
    //         if (config == null)
    //             throw new Exception($"Failed to parse {jsonPath}");

    //         if (behaviorDescriptions.ContainsKey(config.ClassName))
    //             throw new Exception($"Duplicate behavior name: {config.ClassName}");

    //         behaviorDescriptions[config.ClassName] = config;

    //         var dllPath = Path.Combine(
    //             PathService.DLLsDirectory,
    //             "towers",
    //             $"{config.FileName}.dll"
    //          );

    //         if (!File.Exists(dllPath))
    //             throw new FileNotFoundException(dllPath);
    //
    //         var dllBytes = File.ReadAllBytes(dllPath);
    //         var assembly = Assembly.Load(dllBytes);
    //
    //         var type = assembly
    //             .GetTypes()
    //             .FirstOrDefault(t => t.Name == config.ClassName);
    //         if (type == null)
    //             throw new Exception(
    //                 $"Type {config.ClassName} not found in {dllPath}"
    //             );
    //         if (behaviors.ContainsKey(config.ClassName))
    //             throw new Exception($"Duplicate behavior class: {config.ClassName}");

    //         behaviors[config.ClassName] = type;
    //     }
    // }

    private void loadTowersConfigs()
    {
        var result = new Dictionary<string, List<TypeSpecification>>();

        var jsonRoot = Path.Combine(
            PathService.EditorDirectory,
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
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"Skipping empty tower config: {jsonPath}");
                continue;
            }

            List<TypeSpecification> spec = null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    spec = JsonSerializer.Deserialize<List<TypeSpecification>>(json, jsonOptions);
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var singleSpec = JsonSerializer.Deserialize<TypeSpecification>(json, jsonOptions);
                    if (singleSpec != null)
                    {
                        spec = new List<TypeSpecification> { singleSpec };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse tower config {jsonPath}: {ex.Message}. Skipping.");
                continue;
            }

            if (spec == null || spec.Count == 0)
            {
                Console.WriteLine($"Warning: No tower levels or invalid format in {jsonPath}. Skipping.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(spec[0].Name) || string.IsNullOrWhiteSpace(spec[0].ClassName))
            {
                Console.WriteLine($"Skipping invalid tower config without Name/ClassName: {jsonPath}");
                continue;
            }

            var name = spec[0].Name;
            if (result.ContainsKey(name))
            {
                Console.WriteLine($"Warning: Duplicate tower config name '{name}' in {jsonPath}. Skipping.");
                continue;
            }

            result[name] = spec;
        }

        towers = result;
    }
}
