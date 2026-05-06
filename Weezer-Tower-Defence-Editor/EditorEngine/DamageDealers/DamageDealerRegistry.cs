using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SimulationEngine.Infrastructure;
using SimulationEngine.Persistence;

namespace EditorEngine.DamageDealers;


public class DamageDealerRegistry
{
    private static DamageDealerRegistry _instance;
    public static DamageDealerRegistry Instance => _instance ??= new DamageDealerRegistry();
    private DamageDealerRegistry()
    {
        // loadDamageDealerClasses();
        loadDamageDealerConfigs();
    }

    public void Update()
    {
        damageDealers = new();
        loadDamageDealerConfigs();
    }


    public Dictionary<string, BehaviorConfig> behaviorDescriptions = new();
    // public Dictionary<string, Type> behaviors = new();
    public Dictionary<string, TypeSpecification> damageDealers = new();



    /// <summary>
    /// This method loads damage dealer behavior as classes from dll. 
    /// It uses json files to find out which dll to load and which class to look for
    /// Also json-s describe constructor arguments
    /// </summary>
    // private void loadDamageDealerClasses()
    // {

    //     var jsonRoot = System.IO.Path.Combine(
    //         PathService.EditorDirectory,
    //         "custom",
    //         "damageDealers",
    //         "behaviors"
    //     );
    //     var jsonOptions = new JsonSerializerOptions
    //     {
    //         PropertyNameCaseInsensitive = true
    //     };

    //     if (!Directory.Exists(jsonRoot))
    //         "custom",
    //         "damageDealers",
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

    //         var config = JsonSerializer.Deserialize<BehaviorConfig>(json, jsonOptions);
    //         if (config == null)
    //             throw new Exception($"Failed to parse {jsonPath}");

    //         if (behaviorDescriptions.ContainsKey(config.ClassName))
    //             throw new Exception($"Duplicate behavior name: {config.ClassName}");

    //         behaviorDescriptions[config.ClassName] = config;

    //         var dllPath = Path.Combine(
    //             Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    //             "WeezerTowerDefence",
    //             "DLLs",
    //             "damageDealers",
    //             $"{config.FileName}.dll"
    //          );

    //         if (!File.Exists(dllPath))
    //             throw new FileNotFoundException(dllPath);

    //         var assembly = Assembly.LoadFrom(dllPath);

    //         var type = assembly
    //             .GetTypes()
    //             .FirstOrDefault(t => t.Name == config.ClassName);

    //         Console.WriteLine($"OFF COURSE IT IS NOT NONE: {type.Name}");

    //         if (type == null)
    //             throw new Exception(
    //                 $"Type {config.ClassName} not found in {dllPath}"
    //             );
    //         if (behaviors.ContainsKey(config.ClassName))
    //             throw new Exception($"Duplicate behavior class: {config.ClassName}");

    //         behaviors[config.ClassName] = type;
    //     }
    // }

    private void loadDamageDealerConfigs()
    {
        var result = new Dictionary<string, TypeSpecification>();

        var jsonRoot = Path.Combine(
            PathService.EditorDirectory,
            "damageDealers",
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

            Console.WriteLine($"Loaded damage dealer config: {spec}");
            Console.WriteLine($"  Class: {spec.ClassName}");
            // if (!behaviors.ContainsKey(spec.ClassName))
            //     throw new Exception(
            //         $"Behavior class '{spec.ClassName}' not found (config: {jsonPath})"
            //     );

            if (result.ContainsKey(spec.Name))
                throw new Exception($"Duplicate damage dealer config: {spec.Name}");

            result[spec.Name] = spec;
        }

        damageDealers = result;
    }
}





public sealed class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}