using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace EditorEngine.Enemies.Types;

/// <summary>
/// Быстрый враг с меньшим здоровьем
/// </summary>
public class FastEnemyType : IEnemyType
{
    private Texture2D _texture;
    private int _currentWaypointIndex = 0;
    private int _maxHealth = 50;
    private int _health;
    
    public int health { get; set; } = 50;
    public int MaxHealth { get; set; } = 50;
    public float speed { get; set; } = 120f; // В два раза быстрее базового
    public int Damage { get; set; } = 5;
    public float HitRadius { get; set; } = 8f; // Радиус хитбокса (меньше, так как быстрый враг)


    // Fix due to previous incompatibility
    int IEnemyType.MaxHealth => throw new System.NotImplementedException();

    float IEnemyType.HitRadius => throw new System.NotImplementedException();

    public FastEnemyType() : this(null)
    {
        health = 50;
    }

    public FastEnemyType(Texture2D texture)
    {
        _texture = texture;
        health = 50;
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
                Color.Yellow); // Жёлтый цвет для быстрых врагов
        }
        else
        {
            // Простое отображение без текстуры
            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            spriteBatch.Draw(pixel, new Rectangle((int)enemy.Position.X - 8, (int)enemy.Position.Y - 8, 16, 16), Color.Yellow);
        }
    }
}
