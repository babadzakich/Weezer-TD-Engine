using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EditorEngine.Enemies.Behaviors;

/// <summary>
/// Поведение телепортирующегося врага - прыгает вперёд
/// </summary>
public class TeleportingEnemyBehavior : IEnemyBehavior
{
    public string BehaviorId => "teleporting";
    public string BehaviorName => "Teleporting Movement";
    
    private float teleportCooldown = 0f;
    private const float TELEPORT_INTERVAL = 2f;
    
    public void Update(EnemyInstance enemy, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Обычное движение
        enemy.PathProgress += enemy.Config.BaseSpeed * deltaTime * 0.5f;
        
        // Телепорт вперёд каждые 2 секунды
        teleportCooldown -= deltaTime;
        if (teleportCooldown <= 0)
        {
            enemy.PathProgress += 50f; // Прыжок на 50 единиц вперёд
            teleportCooldown = TELEPORT_INTERVAL;
        }
    }
    
    public void Draw(EnemyInstance enemy, SpriteBatch spriteBatch)
    {
        // Фиолетовый ромб для телепортирующегося врага
        var rect = new Rectangle(
            (int)enemy.Position.X - 8,
            (int)enemy.Position.Y - 8,
            16,
            16
        );
    }
}
