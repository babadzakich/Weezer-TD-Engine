using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace SimulationEngine.TowerRelated;

public class Tower
{
    public TowerConfig Config { get; private set; }
    public ITowerBehavior Behavior { get; private set; }
    public Vector2 Position { get; set; }
    public Texture2D Texture { get; set; }
    
    public int UpgradeLevel { get; set; }

    public Tower(TowerConfig config, Vector2 position)
    {
        Config = config;
        Position = position;
        UpgradeLevel = 0;
        
        Behavior = TowerBehaviorRegistry.Instance.Create(config.BehaviorType);
        Behavior.Initialize(this, config);
    }

    public void Update(GameTime gameTime)
    {
        Behavior.Update(this, gameTime);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        Behavior.Draw(this, spriteBatch, Texture);
    }
}