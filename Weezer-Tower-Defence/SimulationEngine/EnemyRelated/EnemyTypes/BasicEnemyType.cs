using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.MapRelated;

namespace SimulationEngine.EnemyRelated.EnemyTypes;

public class BasicEnemyType : IEnemyType
{
    private Texture2D _texture;
    private int _health;
    private float _speed;
    private int _currentWaypointIndex = 0;

    public BasicEnemyType(Texture2D texture = null, float speed = 100f, int health = 100)
    {
        _texture = texture;
        _speed = speed;
        _health = health;
    }
    public void TakeDamage(float amount)
    {
        // Реализация получения урона для базового врага
    }

    public void Update(Enemy enemy, GameTime gameTime, Path path)
    {
        var waypoints = path.GetSmoothPath();
        
        // Если путь пуст или мы уже прошли его
        if (waypoints.Count == 0 || _currentWaypointIndex >= waypoints.Count)
        {
            // Враг достиг конца пути
            // TODO: Нанести урон базе и удалить врага
            return;
        }

        Vector2 target = waypoints[_currentWaypointIndex];
        float distance = Vector2.Distance(enemy.Position, target);
        float moveAmount = _speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

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