using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace SimulationEngine.EnemyRelated.EnemyTypes;

public class BasicEnemyType : IEnemyType
{
    private Texture2D _texture;
    private static Texture2D _placeholderTexture;
    private int _currentWaypointIndex = 0;
    private const int _maxHealth = 100;
    public int health { get; set; } = _maxHealth;
    public int MaxHealth => _maxHealth;

    public float speed => 60f;

    public int Damage => 10; // Базовый враг наносит 10 урона
    
    public float HitRadius => 10f; // Радиус хитбокса базового врага - половинка высоты

    public BasicEnemyType(Texture2D texture = null)
    {
        _texture = texture;
    }
    
    private static Texture2D GetPlaceholderTexture(GraphicsDevice device, Color color)
    {
        if (_placeholderTexture == null)
        {
            _placeholderTexture = new Texture2D(device, 20, 20);
            Color[] data = new Color[20 * 20];
            for (int i = 0; i < data.Length; ++i) data[i] = color;
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
        
        // Если путь пуст или мы уже прошли его - враг достиг точки защиты
        if (waypoints.Count == 0 || _currentWaypointIndex >= waypoints.Count)
        {
            // Враг достиг конца пути, EnemyController нанесет урон базе
            enemy.isAlive = false;
            return;
        }

        Vector2 target = waypoints[_currentWaypointIndex];
        float distance = Vector2.Distance(enemy.Position, target);
        float moveAmount = speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (distance <= moveAmount)
        {
            // Мы достигли точки
            enemy.Position = target;
            _currentWaypointIndex++;
        }
        else
        {
            // Двигаемся к точке
            Vector2 direction = Vector2.Normalize(target - enemy.Position);
            enemy.Position += direction * moveAmount;
        }
    }

    public void Draw(Enemy enemy, SpriteBatch spriteBatch)
    {
        if (_texture != null)
        {
            spriteBatch.Draw(_texture, enemy.Position, Color.White);
        }
        else
        {
            // Рисуем красный квадратик как заглушку
            var placeholder = GetPlaceholderTexture(spriteBatch.GraphicsDevice, Color.Red);
            spriteBatch.Draw(placeholder, enemy.Position, Color.White);
        }
    }
}