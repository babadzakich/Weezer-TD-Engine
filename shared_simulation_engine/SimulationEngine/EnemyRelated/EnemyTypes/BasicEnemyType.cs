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
    public int health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;

    public float speed { get; set; } = 60f;

    public int Damage { get; set; } = 10; // Базовый враг наносит 10 урона
    
    public float HitRadius { get; set; } = 10f; // Радиус хитбокса базового врага - половинка высоты

    public BasicEnemyType(Texture2D texture = null)
    {
        _texture = texture;
    }

    public BasicEnemyType(int health, float speed, int damage, float hitRadius)
    {
        this.health = health;
        this.MaxHealth = health;
        this.speed = speed;
        this.Damage = damage;
        this.HitRadius = hitRadius;
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
            enemy.Velocity = Vector2.Zero;
        }
        else
        {
            // Двигаемся к точке
            Vector2 direction = Vector2.Normalize(target - enemy.Position);
            enemy.Velocity = direction * speed;
            enemy.Position += direction * moveAmount;
        }
    }

    public void Draw(Enemy enemy, SpriteBatch spriteBatch)
    {
        var texture = _texture ?? GetPlaceholderTexture(spriteBatch.GraphicsDevice, Color.Red);
        spriteBatch.Draw(
            texture, 
            enemy.Position, 
            null,
            Color.White,
            0f,
            new Vector2(texture.Width / 2f, texture.Height / 2f),
            1f,
            SpriteEffects.None,
            0f);
    }
}