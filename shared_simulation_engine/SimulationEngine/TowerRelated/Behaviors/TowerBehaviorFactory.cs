using System;
using SimulationEngine;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.TowerRelated;

namespace SimulationEngine.TowerRelated.Behaviors;

public static class TowerBehaviorFactory
{
    public static ITowerBehavior CreateFromRegisteredName(string name)
    {
        if (!TowerBehaviorRegistry.TryGetSpecification(name, out var spec))
        {
            throw new InvalidOperationException($"Tower specification '{name}' is not registered.");
        }

        if (!TowerBehaviorRegistry.TryGetType(spec.ClassName, out var type))
        {
            throw new InvalidOperationException($"Tower behavior type '{spec.ClassName}' is not loaded.");
        }

        if (!TowerBehaviorRegistry.TryGetBehaviorConfig(spec.ClassName, out var behaviorConfig))
        {
            throw new InvalidOperationException($"Behavior config for '{spec.ClassName}' is not loaded.");
        }

        var args = TowerBehaviorRegistry.CreateArgs(behaviorConfig, spec);
        return args.Length == 0
            ? (ITowerBehavior)Activator.CreateInstance(type)
            : (ITowerBehavior)Activator.CreateInstance(type, args);
    }

    public static ITowerBehavior CreateTowerBehavior(LevelLoader.TowerDefinition towerDefinition)
    {
        if (string.IsNullOrEmpty(towerDefinition.ClassName))
        {
            Console.WriteLine($"Tower {towerDefinition.Id} has no ClassName specified. Using default DefinitionTowerBehavior.");
            return new DefinitionTowerBehavior(towerDefinition, CreateProjectileBehavior(towerDefinition));
        }

        try
        {
            Type type = null;

            if (!TowerBehaviorRegistry.TryGetType(towerDefinition.ClassName, out type))
            {
                type = Type.GetType(towerDefinition.ClassName);
            }
            
            if (type == null)
            {
                Console.WriteLine($"Warning: Class {towerDefinition.ClassName} not found for tower {towerDefinition.Id}. Falling back to DefinitionTowerBehavior.");
                return new DefinitionTowerBehavior(towerDefinition, CreateProjectileBehavior(towerDefinition));
            }

            if (!typeof(ITowerBehavior).IsAssignableFrom(type))
            {
                Console.WriteLine($"Warning: Class {towerDefinition.ClassName} does not implement ITowerBehavior. Falling back to DefinitionTowerBehavior.");
                return new DefinitionTowerBehavior(towerDefinition, CreateProjectileBehavior(towerDefinition));
            }

            var projectileBehavior = CreateProjectileBehavior(towerDefinition);
            var behavior = (ITowerBehavior)Activator.CreateInstance(type,
                towerDefinition.Id, towerDefinition.Name,
                projectileBehavior,
                towerDefinition.Cost, towerDefinition.Range, towerDefinition.FireRate);

            behavior.Definition = towerDefinition;

            return behavior;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating behavior for tower {towerDefinition.Id}: {ex.Message}. Falling back to DefinitionTowerBehavior.");
            return new DefinitionTowerBehavior(towerDefinition, CreateProjectileBehavior(towerDefinition));
        }
    }

    private static IDamageDealerBehavior CreateProjectileBehavior(LevelLoader.TowerDefinition towerDefinition)
    {
        if (!string.IsNullOrWhiteSpace(towerDefinition.BulletClassName))
        {
            try
            {
                return DamageDealerRegistry.create(towerDefinition.BulletClassName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Bullet behavior '{towerDefinition.BulletClassName}' not found for tower {towerDefinition.Id}: {ex.Message}");
            }
        }

        var fallbackDamage = towerDefinition.Damage > 0 ? towerDefinition.Damage : 25f;
        return new StandardBulletBehavior(fallbackDamage, 300f, 500f);
    }
}
