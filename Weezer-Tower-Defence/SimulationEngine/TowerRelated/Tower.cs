using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
namespace SimulationEngine.TowerRelated;

public class Tower
{
    public ITowerBehavior Behavior { get; private set; }
    public Vector2 Position { get; set; }
    public Texture2D Texture { get; set; }
    public float cooldownTimer { get; set; } = 0f;
    
    public int UpgradeLevel { get; set; }

    public Tower(ITowerBehavior behavior, Vector2 position)
    {
        Position = position;
        UpgradeLevel = 0;
        Behavior = behavior;
    }

    public void Update(GameTime gameTime)
    {
        Vector2? targetPosition = Behavior.FindTarget(this, EnemyController.GetInstance(null));
        if (targetPosition.HasValue && cooldownTimer <= 0f)
        {
            Behavior.Fire(this, targetPosition.Value);
            cooldownTimer = 1f / Behavior.FireRate;
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
}