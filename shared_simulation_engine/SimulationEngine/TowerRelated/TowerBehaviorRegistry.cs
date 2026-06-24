using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using SimulationEngine.EnemyRelated;
using SimulationEngine.Persistence;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace SimulationEngine.TowerRelated;

public class TowerBehaviorRegistry
{
    private static Dictionary<string, Type> typeRegistry = new();
    private static Dictionary<string, BehaviorConfig> typeBehaviorRegistry = new();
    public static Dictionary<string, TypeSpecification> typeSpecsRegistry = new();


    public static bool TryGetSpecification(string name, out TypeSpecification spec)
    {
        return typeSpecsRegistry.TryGetValue(name, out spec);
    }

    public static bool TryGetType(string className, out Type type)
    {
        return typeRegistry.TryGetValue(className, out type);
    }

    public static bool TryGetBehaviorConfig(string className, out BehaviorConfig config)
    {
        return typeBehaviorRegistry.TryGetValue(className, out config);
    }

    public static void Reset(string dllsDir, string configsDir, string behaviorDescriptionsDir)
    {
        typeRegistry = new();
        typeSpecsRegistry = new();
        typeBehaviorRegistry = loadConfigs(behaviorDescriptionsDir);

        if (!Directory.Exists(configsDir))
        {
            Console.WriteLine($"Warning: Tower configs directory not found: {configsDir}");
            return;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var jsonPath in Directory.EnumerateFiles(configsDir, "*.json"))
        {
            var json = File.ReadAllText(jsonPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"Skipping empty tower config: {jsonPath}");
                continue;
            }

            TypeSpecification spec = null;
            try 
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var specs = JsonSerializer.Deserialize<List<TypeSpecification>>(json, jsonOptions);
                    if (specs != null && specs.Count > 0)
                    {
                        spec = specs[0];
                    }
                    else
                    {
                        Console.WriteLine($"Warning: Tower config {jsonPath} is an empty list. Skipping.");
                        continue;
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    spec = JsonSerializer.Deserialize<TypeSpecification>(json, jsonOptions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse tower config {jsonPath}: {ex.Message}. Skipping.");
                continue;
            }

            if (spec == null)
            {
                Console.WriteLine($"Warning: Could not deserialize tower config {jsonPath}. Skipping.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(spec.Name) || string.IsNullOrWhiteSpace(spec.ClassName))
            {
                Console.WriteLine($"Skipping invalid tower config without Name/ClassName: {jsonPath}");
                continue;
            }

            if (!typeBehaviorRegistry.ContainsKey(spec.ClassName))
            {
                Console.WriteLine($"Warning: Behavior class '{spec.ClassName}' not found (config: {jsonPath}). Skipping.");
                continue;
            }

            if (typeSpecsRegistry.ContainsKey(spec.Name))
            {
                Console.WriteLine($"Warning: Duplicate tower config name: {spec.Name} (file: {jsonPath}). Skipping.");
                continue;
            }

            typeSpecsRegistry[spec.Name] = spec;

            var dllPath = Path.Combine(dllsDir, typeBehaviorRegistry[spec.ClassName].FileName + ".dll");
            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"Warning: DLL not found for tower behavior '{spec.ClassName}': {dllPath}. Skipping.");
                continue;
            }

            var dllBytes = File.ReadAllBytes(dllPath);
            var assembly = Assembly.Load(dllBytes);
            Type type = null;
            try
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == spec.ClassName);
            }
            catch (ReflectionTypeLoadException ex)
            {
                type = ex.Types.FirstOrDefault(t => t != null && t.Name == spec.ClassName);
            }

            if (type == null)
                throw new Exception($"Type '{spec.ClassName}' not found in assembly '{dllPath}'");
            typeRegistry[spec.ClassName] = type;
        }
    }

    public static object[] CreateArgs(BehaviorConfig bc, TypeSpecification ts)
    {
        List<object> args = new();
        foreach (var argSpec in ts.Args)
        {
            var argConfig = bc.Args.FirstOrDefault(a => a.Name == argSpec.Name);
            if (argConfig == null)
                throw new Exception(
                    $"Argument '{argSpec.Name}' not found in behavior config '{bc.Name}'"
                );
            object argValue = argSpec.Value switch
            {
                JsonElement je when je.ValueKind == JsonValueKind.Number && argConfig.Type == "int" =>
                    je.GetInt32(),
                JsonElement je when je.ValueKind == JsonValueKind.Number && argConfig.Type == "float" =>
                    je.GetSingle(),
                JsonElement je when je.ValueKind == JsonValueKind.String && argConfig.Type == "IDamageDealerBehavior" =>
                    BulletRelated.DamageDealerRegistry.create(je.GetString()!),
                JsonElement je when je.ValueKind == JsonValueKind.String && argConfig.Type == "string" =>
                    je.GetString()!,
                JsonElement je when je.ValueKind == JsonValueKind.True && argConfig.Type == "bool" =>
                    true,
                JsonElement je when je.ValueKind == JsonValueKind.False && argConfig.Type == "bool" =>
                    false,
                _ => throw new Exception(
                    $"Unsupported argument type '{argConfig.Type}' for argument '{argSpec.Name}'; Tower Type {ts.Name}"
                ),
            };
            args.Add(argValue);
        }

        return args.ToArray();
    }

    private static Dictionary<string, BehaviorConfig> loadConfigs(string behaviorDescriptionsDir)
    {
        Dictionary<string, BehaviorConfig> result = new();

        if (!Directory.Exists(behaviorDescriptionsDir))
        {
            Console.WriteLine($"Warning: Tower behavior descriptions directory not found: {behaviorDescriptionsDir}");
            return result;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var jsonPath in Directory.EnumerateFiles(behaviorDescriptionsDir, "*.json"))
        {
            var json = File.ReadAllText(jsonPath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"Skipping empty tower behavior config: {jsonPath}");
                continue;
            }

            var spec = JsonSerializer.Deserialize<BehaviorConfig>(json, jsonOptions);
            if (spec == null)
                throw new Exception($"Failed to parse {jsonPath}");

            if (string.IsNullOrWhiteSpace(spec.ClassName) || string.IsNullOrWhiteSpace(spec.FileName))
            {
                Console.WriteLine($"Skipping invalid tower behavior config without ClassName/FileName: {jsonPath}");
                continue;
            }

            result[spec.ClassName] = spec;
        }

        return result;
    }

}
