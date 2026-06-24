using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EditorEngine.Enemies;

public interface IEnemyBehavior
{
    string BehaviorId { get; }
    string BehaviorName { get; }
    
    void Update(EnemyInstance enemy, GameTime gameTime);
    void Draw(EnemyInstance enemy, SpriteBatch spriteBatch);
}

public class EnemyConfig
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string BehaviorId { get; set; }
    public int BaseHealth { get; set; }
    public float BaseSpeed { get; set; }
    public int Damage { get; set; }
}

public class EnemyInstance
{
    public EnemyConfig Config { get; set; }
    public IEnemyBehavior Behavior { get; set; }
    
    public int CurrentHealth { get; set; }
    public Vector2 Position { get; set; }
    public float PathProgress { get; set; }
    
    public EnemyInstance(EnemyConfig config, IEnemyBehavior behavior)
    {
        Config = config;
        Behavior = behavior;
        CurrentHealth = config.BaseHealth;
    }
    
    public void TakeDamage(int amount)
    {
        CurrentHealth -= amount;
    }
    
    public bool IsAlive => CurrentHealth > 0;
}
