using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace EditorEngine.Enemies.Types;

/// <summary>
/// Танк - медленный враг с большим здоровьем
/// </summary>
public class TankEnemyType : IEnemyType
{
    private Texture2D _texture;
    private int _currentWaypointIndex = 0;
    
    public int health { get; set; } = 300;
    public float speed => 30f; // Медленный
    public int Damage => 20; // Наносит больше урона

    // Fix due to previous incompatibility
    int IEnemyType.MaxHealth => throw new System.NotImplementedException();

    float IEnemyType.HitRadius => throw new System.NotImplementedException();

    public TankEnemyType() : this(null)
    {
    }

    public TankEnemyType(Texture2D texture)
    {
        _texture = texture;
    }

    public void SetTexture(Texture2D texture)
    {
        _texture = texture;
    }

    public void TakeDamage(float amount)
    {
        health -= (int)amount;
    }

    public void Update(Enemy enemy, GameTime gameTime, Path path)
    {
        var waypoints = path.GetSmoothPath();
        
        if (waypoints.Count == 0 || _currentWaypointIndex >= waypoints.Count)
        {
            enemy.isAlive = false;
            return;
        }

        Vector2 target = waypoints[_currentWaypointIndex];
        float distance = Vector2.Distance(enemy.Position, target);
        float moveAmount = speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (distance <= moveAmount)
        {
            enemy.Position = target;
            _currentWaypointIndex++;
        }
        else
        {
            Vector2 direction = Vector2.Normalize(target - enemy.Position);
            enemy.Position += direction * moveAmount;
        }
    }

    public void Draw(Enemy enemy, SpriteBatch spriteBatch)
    {
        if (_texture != null)
        {
            spriteBatch.Draw(_texture, 
                enemy.Position - new Vector2(_texture.Width / 2, _texture.Height / 2), 
                Color.DarkRed); // Тёмно-красный для танков
        }
        else
        {
            // Простое отображение без текстуры - большой квадрат
            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Draw(pixel, new Rectangle((int)enemy.Position.X - 12, (int)enemy.Position.Y - 12, 24, 24), Color.DarkRed);
        }
    }
}
