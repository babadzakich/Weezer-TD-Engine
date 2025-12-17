using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace SimulationEngine.EnemyRelated.EnemyTypes;

/// <summary>
/// Быстрый враг с меньшим здоровьем
/// </summary>
public class FastEnemyType : IEnemyType
{
    private Texture2D _texture;
    private static Texture2D _placeholderTexture;
    private int _currentWaypointIndex = 0;
    
    public int health { get; set; } = 50;
    public float speed => 120f; // В два раза быстрее базового
    public int Damage => 5;

    public FastEnemyType(Texture2D texture = null)
    {
        _texture = texture;
    }
    
    private static Texture2D GetPlaceholderTexture(GraphicsDevice device)
    {
        if (_placeholderTexture == null)
        {
            _placeholderTexture = new Texture2D(device, 16, 16);
            Color[] data = new Color[16 * 16];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.Yellow;
            _placeholderTexture.SetData(data);
        }
        return _placeholderTexture;
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
                Color.White);
        }
        else
        {
            // Жёлтый квадратик для быстрых врагов
            var placeholder = GetPlaceholderTexture(spriteBatch.GraphicsDevice);
            spriteBatch.Draw(placeholder, enemy.Position - new Vector2(8, 8), Color.White);
        }
    }
}
