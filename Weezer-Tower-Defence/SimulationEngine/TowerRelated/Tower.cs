using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace SimulationEngine.TowerRelated;

public class Tower
{
    public ITowerBehavior Behavior { get; private set; }
    public Vector2 Position { get; set; }
    public Texture2D Texture { get; set; }
    
    public int UpgradeLevel { get; set; }

    public Tower(ITowerBehavior behavior, Vector2 position)
    {
        Position = position;
        UpgradeLevel = 0;
        Behavior = behavior;
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