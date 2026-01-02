using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace SimulationEngine.EnemyRelated.EnemyTypes;

/// <summary>
/// Танк - медленный враг с большим здоровьем
/// </summary>
public class TankEnemyType : IEnemyType
{
    private Texture2D _texture;
    private static Texture2D _placeholderTexture;
    private int _currentWaypointIndex = 0;
    private const int _maxHealth = 300;
    
    public int health { get; set; } = _maxHealth;
    public int MaxHealth => _maxHealth;
    public float speed => 30f; // Медленнее базового
    public int Damage => 20; // Танк наносит больше урона

    public TankEnemyType(Texture2D texture = null)
    {
        _texture = texture;
    }
    
    private static Texture2D GetPlaceholderTexture(GraphicsDevice device)
    {
        if (_placeholderTexture == null)
        {
            _placeholderTexture = new Texture2D(device, 24, 24);
            Color[] data = new Color[24 * 24];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.DarkRed;
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
            // Тёмно-красный большой квадрат для танков
            var placeholder = GetPlaceholderTexture(spriteBatch.GraphicsDevice);
            spriteBatch.Draw(placeholder, enemy.Position - new Vector2(12, 12), Color.White);
        }
    }
}
