using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;


public class Enemy
{
    public Vector2 Position { get; set; }
    private IEnemyType _type;
    private readonly SimulationEngine.MapRelated.Path _path;
    public Enemy(IEnemyType enemyType, Vector2 position, SimulationEngine.MapRelated.Path path)
    {
        _type = enemyType;
        Position = position;
        _path = path;
    }

    /**
    * Move creature on map
    */
    public void Update(GameTime gameTime)
    {
        _type.Update(this, gameTime, _path);
        
    }
    public void Draw(SpriteBatch sb)
    {
        _type.Draw(this, sb);
    }

    public void TakeDamage(float amount)
    {
        _type.TakeDamage(amount);
    }
}