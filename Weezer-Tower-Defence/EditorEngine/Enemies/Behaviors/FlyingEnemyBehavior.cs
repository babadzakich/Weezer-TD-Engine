using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EditorEngine.Enemies.Behaviors;

/// <summary>
/// Поведение летающего врага - может пролетать через препятствия
/// </summary>
public class FlyingEnemyBehavior : IEnemyBehavior
{
    public string BehaviorId => "flying";
    public string BehaviorName => "Flying Movement";
    
    public void Update(EnemyInstance enemy, GameTime gameTime)
    {
        // Летит прямо к цели, игнорируя пути
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        enemy.PathProgress += enemy.Config.BaseSpeed * deltaTime * 1.2f; // На 20% быстрее
    }
    
    public void Draw(EnemyInstance enemy, SpriteBatch spriteBatch)
    {
        // Синий треугольник для летающего врага
        var rect = new Rectangle(
            (int)enemy.Position.X - 12,
            (int)enemy.Position.Y - 12,
            24,
            24
        );
    }
}
