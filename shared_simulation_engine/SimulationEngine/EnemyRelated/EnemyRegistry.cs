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
using Microsoft.Xna.Framework.Graphics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace SimulationEngine.EnemyRelated;
public class EnemyRegistry
{
    private static Dictionary<string, Type> typeRegistry = new();
    private static Dictionary<string, BehaviorConfig> typeBehaviorRegistry = new();
    private static Dictionary<string, TypeSpecification> typeSpecsRegistry = new();
    public static Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice { get; set; }
    public static Texture2D DefaultTexture { get; set; }

    public static IEnemyType create(string name)
    {
        if (!typeSpecsRegistry.TryGetValue(name, out var spec))
        {
            // Пытаемся найти встроенный тип
            var builtInType = FindBuiltInType(name);
            if (builtInType != null)
            {
                var builtInInstance = (IEnemyType)Activator.CreateInstance(builtInType);
                ApplyTexture(name, builtInInstance);
                return builtInInstance;
            }
            throw new Exception($"Enemy config or built-in type not found: {name}");
        }

        var type = typeRegistry[spec.ClassName];
        var behavior = typeBehaviorRegistry[spec.ClassName];

        IEnemyType instance;
        if (spec.Args.Count == 0)
            instance = (IEnemyType)Activator.CreateInstance(type);
        else
            instance = (IEnemyType)Activator.CreateInstance(type, createArgs(behavior, spec));

        ApplyTexture(name, instance);

        return instance;
    }

    private static void ApplyTexture(string name, IEnemyType instance)
    {
        // Попытка загрузить текстуру для врага
        if (GraphicsDevice != null)
        {
            var texture = LoadEnemyTexture(name) ?? DefaultTexture;
            if (texture != null)
            {
                instance.SetTexture(texture);
            }
        }
    }

    private static Type FindBuiltInType(string name)
    {
        var assembly = typeof(EnemyRegistry).Assembly;
        var typeName = name.ToLower();
        
        // Ищем по имени класса (напр. GoblinEnemyType) или по ID (goblin)
        return assembly.GetTypes()
            .FirstOrDefault(t => typeof(IEnemyType).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract &&
                (t.Name.ToLower() == typeName + "enemytype" || t.Name.ToLower() == typeName + "type" || t.Name.ToLower() == typeName));
    }

    private static Texture2D LoadEnemyTexture(string name)
    {
        // Путь к текстуре в папке common/enemies/
        string commonDir = Infrastructure.PathService.CommonDirectory;
        string enemiesDir = Path.Combine(commonDir, "enemies");
        
        if (!Directory.Exists(enemiesDir))
            Directory.CreateDirectory(enemiesDir);

        string[] extensions = { ".png", ".jpg", ".bmp" };
        foreach (var ext in extensions)
        {
            string path = Path.Combine(enemiesDir, name + ext);
            if (File.Exists(path))
            {
                using (var stream = File.OpenRead(path))
                {
                    return Texture2D.FromStream(GraphicsDevice, stream);
                }
            }
        }

        return null;
    }

    public static void ResetEnemies(string dllsDir, string configsDir, string behaviorDescriptionsDir)
    {
        typeRegistry = new();
        typeSpecsRegistry = new();
        typeBehaviorRegistry = loadConfigs(behaviorDescriptionsDir);

        if (!Directory.Exists(configsDir))
        {
            Console.WriteLine($"Warning: Enemy configs directory not found: {configsDir}");
            return;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        foreach (var jsonPath in Directory.EnumerateFiles(configsDir, "*.json"))
        {
            var json = File.ReadAllText(jsonPath);

            TypeSpecification spec = null;
            try
            {
                spec = JsonSerializer.Deserialize<TypeSpecification>(json, jsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse enemy config {jsonPath}: {ex.Message}. Skipping.");
                continue;
            }

            if (spec == null || string.IsNullOrWhiteSpace(spec.Name) || string.IsNullOrWhiteSpace(spec.ClassName))
            {
                Console.WriteLine($"Warning: Invalid enemy config in {jsonPath}. Skipping.");
                continue;
            }

            if (!typeBehaviorRegistry.ContainsKey(spec.ClassName))
            {
                Console.WriteLine($"Warning: Behavior class '{spec.ClassName}' not found (config: {jsonPath}). Skipping.");
                continue;
            }

            if (typeSpecsRegistry.ContainsKey(spec.Name))
            {
                Console.WriteLine($"Warning: Duplicate enemy config: {spec.Name}. Skipping.");
                continue;
            }

            typeSpecsRegistry[spec.Name] = spec;

            var dllPath = Path.Combine(dllsDir, typeBehaviorRegistry[spec.ClassName].FileName + ".dll");
            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"Warning: DLL not found for enemy behavior '{spec.ClassName}': {dllPath}. Skipping.");
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

        if (!Directory.Exists(behaviorDescriptionsDir))
        {
            Console.WriteLine($"Warning: Enemy behavior descriptions directory not found: {behaviorDescriptionsDir}");
            return result;
        }

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
