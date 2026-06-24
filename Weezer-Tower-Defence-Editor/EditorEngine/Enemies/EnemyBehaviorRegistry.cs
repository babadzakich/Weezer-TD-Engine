using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EditorEngine.Enemies;

/// <summary>
/// Реестр поведений врагов - загружает все IEnemyBehavior из EditorEngine.Enemies.Behaviors
/// </summary>
public class EnemyBehaviorRegistry
{
    private static EnemyBehaviorRegistry _instance;
    public static EnemyBehaviorRegistry Instance => _instance ??= new EnemyBehaviorRegistry();

    private Dictionary<string, IEnemyBehavior> behaviors = new();

    private EnemyBehaviorRegistry()
    {
        LoadBehaviors();
    }

    private void LoadBehaviors()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var behaviorTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null &&
                   t.Namespace.StartsWith("EditorEngine.Enemies.Behaviors") &&
                   typeof(IEnemyBehavior).IsAssignableFrom(t) &&
                   !t.IsInterface &&
                   !t.IsAbstract);

        foreach (var type in behaviorTypes)
        {
            try
            {
                var instance = (IEnemyBehavior)Activator.CreateInstance(type);
                behaviors[instance.BehaviorId] = instance;
                Console.WriteLine($"Loaded enemy behavior: {instance.BehaviorName} (ID: {instance.BehaviorId})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load behavior {type.Name}: {ex.Message}");
            }
        }
    }

    public IEnemyBehavior GetBehavior(string behaviorId) => behaviors.GetValueOrDefault(behaviorId);
    public List<IEnemyBehavior> GetAllBehaviors() => behaviors.Values.ToList();
    public List<string> GetAllBehaviorIds() => behaviors.Keys.ToList();
}
