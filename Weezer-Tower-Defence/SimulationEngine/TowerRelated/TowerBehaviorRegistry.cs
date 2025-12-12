using System;
using System.Collections.Generic;
using SimulationEngine.TowerRelated.Behaviors;

namespace SimulationEngine.TowerRelated;

/// <summary>
/// Реестр поведений башен - связывает строковые ID с классами поведений
/// </summary>
public class TowerBehaviorRegistry
{
    private static TowerBehaviorRegistry _instance;
    private Dictionary<string, Func<ITowerBehavior>> _behaviorFactories;

    private TowerBehaviorRegistry()
    {
        _behaviorFactories = new Dictionary<string, Func<ITowerBehavior>>();
        RegisterDefaultBehaviors();
    }

    public static TowerBehaviorRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = new TowerBehaviorRegistry();
            return _instance;
        }
    }

    /// <summary>
    /// Регистрация стандартных поведений
    /// </summary>
    private void RegisterDefaultBehaviors()
    {
        Register("BasicTowerBehavior", () => new BasicTowerBehavior("basic_tower", "Basic Tower", new SimulationEngine.BulletRelated.Behaviors.StandardBulletBehavior(25f, 300f, 500f, null), 100, 150f, 1f));
        Register("LaserTowerBehavior", () => new LaserTowerBehavior());
        // Другие поведения можно добавить позже или через плагины
    }

    /// <summary>
    /// Регистрация нового поведения (для плагинов)
    /// </summary>
    public void Register(string behaviorType, Func<ITowerBehavior> factory)
    {
        _behaviorFactories[behaviorType] = factory;
    }

    /// <summary>
    /// Создать экземпляр поведения по типу
    /// </summary>
    public ITowerBehavior Create(string behaviorType)
    {
        if (string.IsNullOrEmpty(behaviorType))
        {
            // По умолчанию базовое поведение
            return new BasicTowerBehavior("basic_tower", "Basic Tower", new SimulationEngine.BulletRelated.Behaviors.StandardBulletBehavior(25f, 300f, 500f, null), 100, 150f, 1f);
        }

        if (_behaviorFactories.TryGetValue(behaviorType, out var factory))
        {
            return factory();
        }

        throw new ArgumentException($"Unknown tower behavior type: {behaviorType}");
    }

    /// <summary>
    /// Проверить, зарегистрирован ли тип поведения
    /// </summary>
    public bool IsRegistered(string behaviorType)
    {
        return _behaviorFactories.ContainsKey(behaviorType);
    }
}
