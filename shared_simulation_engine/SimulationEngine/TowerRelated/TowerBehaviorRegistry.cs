using System;
using System.Collections.Generic;
using SimulationEngine.TowerRelated.Behaviors;

namespace SimulationEngine.TowerRelated;

/// <summary>
/// Реестр поведений башен - связывает строковые ID с классами поведений
/// 
/// Класс нигде не используется, поэтому лучше использовать Factory, там не надо ничего регистрировать вручную
/// </summary>
public class TowerBehaviorRegistry
{
    private static TowerBehaviorRegistry _instance;
    private Dictionary<string, TypeSpecification> _behaviorFactories = new();

    private TowerBehaviorRegistry()
    {
        _behaviorFactories = new Dictionary<string, TypeSpecification>();
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


}
