using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;

namespace MyPlugins.DamageDealers;

public class StandardBehavior : IDamageDealerBehavior
{
    private float _speed;
    private float _damage;
    private float _maxDistance;

    private Texture2D _texture;

    // ОБЯЗАТЕЛЬНО пустой конструктор
    public StandardBehavior() { }

    // Конфигурация из JSON / runtime
    public void Configure(float damage, float speed, float maxDistance)
    {
        _damage = damage;
        _speed = speed;
        _maxDistance = maxDistance;
    }

    public float Damage => _damage;

    public void Update(DamageDealer bullet, GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bullet.Position += bullet.Direction * _speed * dt;

        float traveled = Vector2.Distance(bullet.StartPosition, bullet.Position);
        if (traveled >= _maxDistance)
            bullet.IsActive = false;
    }

    public void Draw(DamageDealer bullet, SpriteBatch spriteBatch)
    {
        if (_texture == null)
        {
            _texture = new Texture2D(spriteBatch.GraphicsDevice, 6, 6);
            var data = new Color[6 * 6];
            for (int i = 0; i < data.Length; i++)
                data[i] = Color.White;
            _texture.SetData(data);
        }

        spriteBatch.Draw(
            _texture,
            bullet.Position,
            null,
            Color.White,
            0f,
            new Vector2(_texture.Width / 2f, _texture.Height / 2f),
            1f,
            SpriteEffects.None,
            0f
        );
    }
}
