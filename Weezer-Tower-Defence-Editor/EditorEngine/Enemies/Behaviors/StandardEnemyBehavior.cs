using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using EditorEngine.Enemies;

/// <summary>
/// Стандартное поведение - просто идёт по пути
/// </summary>
public class StandardEnemyBehavior : IEnemyBehavior
{
    public string BehaviorId => "standard";
    public string BehaviorName => "Standard Movement";
    
    public void Update(EnemyInstance enemy, GameTime gameTime)
    {
        // Движение по пути
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        enemy.PathProgress += enemy.Config.BaseSpeed * deltaTime;
    }
    
    public void Draw(EnemyInstance enemy, SpriteBatch spriteBatch)
    {
        // Простой красный квадрат
        var rect = new Rectangle(
            (int)enemy.Position.X - 10,
            (int)enemy.Position.Y - 10,
            20,
            20
        );
        
        // Используем белый пиксель из spriteBatch (нужен Texture2D)
        // Пока просто заглушка - в реальности нужна текстура
    }
}
