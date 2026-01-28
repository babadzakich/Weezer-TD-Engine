using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.EnemyRelated;

namespace SimulationEngine.TowerRelated;


public interface ITowerBehavior
{
    string Id { get; }
    string Name { get; }
    int Cost { get; }
    float Range { get; }
    float FireRate { get; }
    LevelLoader.TowerDefinition Definition { get; set; }

    Vector2? FindTarget(Tower tower, EnemyController enemies);
    
    void Fire(Tower tower, Vector2 targetPosition);
   
    void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture);
}