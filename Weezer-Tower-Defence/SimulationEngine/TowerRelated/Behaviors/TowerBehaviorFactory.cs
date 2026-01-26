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
        if (string.IsNullOrEmpty(towerDefinition.ClassName))
        {
            Console.WriteLine($"Tower {towerDefinition.Id} has no ClassName specified. Using default DefinitionTowerBehavior.");
            return new DefinitionTowerBehavior(towerDefinition, new StandardBulletBehavior(25f, 300f, 500f));
        }

        try
        {
            var type = Type.GetType(towerDefinition.ClassName);
            
            if (type == null)
            {
                Console.WriteLine($"Warning: Class {towerDefinition.ClassName} not found for tower {towerDefinition.Id}. Falling back to DefinitionTowerBehavior.");
                return new DefinitionTowerBehavior(towerDefinition, new StandardBulletBehavior(25f, 300f, 500f));
            }

            if (!typeof(ITowerBehavior).IsAssignableFrom(type))
            {
                Console.WriteLine($"Warning: Class {towerDefinition.ClassName} does not implement ITowerBehavior. Falling back to DefinitionTowerBehavior.");
                return new DefinitionTowerBehavior(towerDefinition, new StandardBulletBehavior(25f, 300f, 500f));
            }

            var behavior = (ITowerBehavior)Activator.CreateInstance(type,
                towerDefinition.Id, towerDefinition.Name,
                new StandardBulletBehavior(25f, 300f, 500f),
                towerDefinition.Cost, towerDefinition.Range, towerDefinition.FireRate);

            behavior.Definition = towerDefinition;

            return behavior;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating behavior for tower {towerDefinition.Id}: {ex.Message}. Falling back to DefinitionTowerBehavior.");
            return new DefinitionTowerBehavior(towerDefinition, new StandardBulletBehavior(25f, 300f, 500f));
        }
    }
}
