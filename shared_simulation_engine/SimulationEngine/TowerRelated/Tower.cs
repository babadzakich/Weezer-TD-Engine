using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine;
namespace SimulationEngine.TowerRelated;

public class Tower
{
    public ITowerBehavior Behavior { get; private set; }
    public LevelLoader.TowerDefinition Definition { get; }
    public Vector2 Position { get; set; }
    public Texture2D Texture { get; set; }
    public float cooldownTimer { get; set; } = 0f;
    
    public int UpgradeLevel { get; set; }

    public Tower(ITowerBehavior behavior, Vector2 position, LevelLoader.TowerDefinition definition = null)
    {
        Position = position;
        UpgradeLevel = 0;
        Behavior = behavior;
        Definition = definition ?? behavior.Definition;
        ApplyLevelStats();
    }

    public void Update(GameTime gameTime)
    {
        var enemyController = GameManager.GetInstance().EnemyController;
        if (enemyController != null)
        {
            Vector2? targetPosition = Behavior.FindTarget(this, enemyController);
            if (targetPosition.HasValue && cooldownTimer <= 0f)
            {
                Behavior.Fire(this, targetPosition.Value);
                cooldownTimer = 1f / Behavior.FireRate;
            }
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        Behavior.Draw(this, spriteBatch, Texture);
    }

    public int? GetNextUpgradeCost()
    {
        if (Definition == null || Definition.UpgradeLevels == null) return null;
        int nextIndex = UpgradeLevel;
        if (nextIndex < 0 || nextIndex >= Definition.UpgradeLevels.Count) return null;
        return Definition.UpgradeLevels[nextIndex].UpgradeCost;
    }

    public bool ApplyLevelStats()
    {
        // If behavior supports dynamic levels, update its public stats
        if (Behavior is SimulationEngine.TowerRelated.Behaviors.DefinitionTowerBehavior defBehavior)
        {
            defBehavior.ApplyLevel(UpgradeLevel);
            return true;
        }
        if (Behavior is SimulationEngine.TowerRelated.Behaviors.BasicTowerBehavior basicBehavior)
        {
            basicBehavior.ApplyLevel(UpgradeLevel);
            return true;
        }
        return false;
    }
}