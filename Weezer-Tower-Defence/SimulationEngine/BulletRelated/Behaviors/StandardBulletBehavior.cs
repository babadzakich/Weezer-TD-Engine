using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.BulletRelated.Behaviors
{
    public class StandardBulletBehavior : IDamageDealerBehavior
    {
        private float _speed;
        private float _damage;
        private float _maxDistance;

        private Texture2D _texture;

        public StandardBulletBehavior(float damage, float speed, float maxDistance, Texture2D texture)
        {
            _damage = damage;
            _speed = speed;
            _maxDistance = maxDistance;
            _texture = texture;
        }

        public void Draw(DamageDealer damageDealer, SpriteBatch spriteBatch)
        {
            if (_texture == null) _texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            spriteBatch.Draw(_texture, damageDealer.Position, Color.White);
        }

        public void Update(DamageDealer bullet, GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bullet.Position += bullet.Direction * _speed * deltaTime;
            
            // Basic bounds check or logic could go here, 
            // but usually the controller handles removal if out of bounds.
            // For now, just movement.
        }

        private void onHitTarget(DamageDealer bullet/*, EnemyController targets*/)
        {
            // Logic for when the bullet hits a target
            // e.g., apply damage, play effects, deactivate bullet, etc.
            bullet.IsActive = false;
        }
    }
}
