using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.EnemyRelated;

public class Enemy
{
    public Vector2 Position { get; set; }
    private IEnemyType _type;
    private readonly MapRelated.Path _path;
    public int Health => _type.health;
    public int Damage => _type.Damage;
    public bool isAlive { get; set; } = true;
    
    public string GetDefensePointId() => _path.DefensePointId;
    
    public Enemy(IEnemyType enemyType, Vector2 position, MapRelated.Path path)
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