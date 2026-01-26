using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;

namespace EditorEngine.Enemies.Types;

public class BasicEnemyType : IEnemyType
{
    private Texture2D _texture;
    private int _currentWaypointIndex = 0;
    private int _maxHealth = 100;
    private int _health;
    
    public int health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public float speed { get; set; } = 60f;
    public int Damage { get; set; } = 10; // Базовый враг наносит 10 урона
    public float HitRadius { get; set; } = 10f; // Радиус хитбокса


    // Fix due to previous incompatibility
    int IEnemyType.MaxHealth => throw new System.NotImplementedException();

    float IEnemyType.HitRadius => throw new System.NotImplementedException();

    public BasicEnemyType() : this(null)
    {
        health = 100;
    }

    public BasicEnemyType(Texture2D texture)
    {
        _texture = texture;
        health = 100;
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
        } else {
            // Рисуем заглушку, если текстура не установлена
            Texture2D placeholder = new Texture2D(spriteBatch.GraphicsDevice, 20, 20);
            Color[] data = new Color[20 * 20];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.Red;
            placeholder.SetData(data);
            spriteBatch.Draw(placeholder, enemy.Position, Color.White);
        }
    }
}