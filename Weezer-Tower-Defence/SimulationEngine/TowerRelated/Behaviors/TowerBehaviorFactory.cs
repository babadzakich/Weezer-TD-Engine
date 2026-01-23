using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimulationEngine;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;


namespace SimulationEngine.TowerRelated.Behaviors;

class TowerBehaviorFactory
{
    public static ITowerBehavior CreateTowerBehavior(LevelLoader.TowerDefinition towerDefinition)
    {
        var type = Type.GetType(towerDefinition.ClassName)
            ?? throw new Exception($"Class specified for tower behaviour is not found {towerDefinition.ClassName}");

        if (!typeof(ITowerBehavior).IsAssignableFrom(type))
            throw new Exception("Class specified for tower behavious is found but doesn't implement ITowerBehavior");

        return (ITowerBehavior)Activator.CreateInstance(type,
            towerDefinition.Id, towerDefinition.Name,
            new StandardBulletBehavior(25f, 300f, 500f),
            towerDefinition.Cost, towerDefinition.Range, towerDefinition.FireRate);
    }
}
