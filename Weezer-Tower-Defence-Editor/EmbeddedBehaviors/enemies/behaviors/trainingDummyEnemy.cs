using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.MapRelated;

namespace SimulationEngine.EnemyRelated.EnemyTypes;

public class TrainingDummyEnemyType : IEnemyType
{
    private static Texture2D _placeholderTexture;
    private int _currentWaypointIndex;

    public int health { get; set; }
    public int MaxHealth { get; set; }
    public float speed { get; set; }
    public int Damage { get; set; }
    public float HitRadius { get; set; }

    public TrainingDummyEnemyType(int health, float speed, int damage, float hitRadius)
    {
        MaxHealth = health;
        this.health = health;
        this.speed = speed;
        Damage = damage;
        HitRadius = hitRadius;
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
            enemy.Velocity = Vector2.Zero;
        }
        else
        {
            Vector2 direction = Vector2.Normalize(target - enemy.Position);
            enemy.Velocity = direction * speed;
            enemy.Position += direction * moveAmount;
        }
    }

    public void Draw(Enemy enemy, SpriteBatch spriteBatch)
    {
        var texture = GetPlaceholderTexture(spriteBatch.GraphicsDevice);
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

    private static Texture2D GetPlaceholderTexture(GraphicsDevice device)
    {
        if (_placeholderTexture != null)
        {
            return _placeholderTexture;
        }

        const int size = 24;
        _placeholderTexture = new Texture2D(device, size, size);
        var data = new Color[size * size];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = new Color(200, 80, 80);
        }

        _placeholderTexture.SetData(data);
        return _placeholderTexture;
    }
}
