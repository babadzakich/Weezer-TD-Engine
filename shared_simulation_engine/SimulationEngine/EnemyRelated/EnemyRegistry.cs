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

namespace SimulationEngine.EnemyRelated;
public class EnemyRegistry
{

    public static Dictionary<string, IEnemyType> enemies = new();

    public static void ResetEnemies(string dllsDir, string configsDir, string behaviorDescriptionsDir)
    {
        Dictionary<string, BehaviorConfig> behaviors = loadConfigs(behaviorDescriptionsDir);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var jsonPath in Directory.EnumerateFiles(configsDir, "*.json"))
        {
            var json = File.ReadAllText(jsonPath);

            var spec = JsonSerializer.Deserialize<TypeSpecification>(json, jsonOptions);
            if (spec == null)
                throw new Exception($"Failed to parse {jsonPath}");

            if (!behaviors.ContainsKey(spec.ClassName))
                throw new Exception(
                    $"Behavior class '{spec.ClassName}' not found (config: {jsonPath})"
                );

            if (enemies.ContainsKey(spec.Name))
                throw new Exception($"Duplicate damage dealer config: {spec.Name}");


            var dllPath = Path.Combine(dllsDir, behaviors[spec.ClassName].FileName + ".dll");
            var assembly = Assembly.LoadFrom(dllPath);
            var type = assembly.GetType(spec.ClassName);
            if (type == null)
                throw new Exception($"Type '{spec.ClassName}' not found in assembly '{dllPath}'");
            var instance = (IEnemyType)Activator.CreateInstance(type, createArgs(behaviors[spec.ClassName], spec));

            enemies[spec.Name] = instance;
        }
    }

    private static object[] createArgs(BehaviorConfig bc, TypeSpecification ts)
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
                JsonElement je when je.ValueKind == JsonValueKind.String && argConfig.Type == "string" =>
                    je.GetString()!,
                JsonElement je when je.ValueKind == JsonValueKind.True && argConfig.Type == "bool" =>
                    true,
                JsonElement je when je.ValueKind == JsonValueKind.False && argConfig.Type == "bool" =>
                    false,
                _ => throw new Exception(
                    $"Unsupported argument type '{argConfig.Type}' for argument '{argSpec.Name}'"
                ),
            };
            args.Add(argValue);
        }

        return args.ToArray();
    }

    private static Dictionary<string, BehaviorConfig> loadConfigs(string behaviorDescriptionsDir)
    {
        Dictionary<string, BehaviorConfig> result = new();

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var jsonPath in Directory.EnumerateFiles(behaviorDescriptionsDir, "*.json"))
        {
            var json = File.ReadAllText(jsonPath);

            var spec = JsonSerializer.Deserialize<BehaviorConfig>(json, jsonOptions);
            if (spec == null)
                throw new Exception($"Failed to parse {jsonPath}");

            result[spec.ClassName] = spec;
        }

        return result;
    }

}