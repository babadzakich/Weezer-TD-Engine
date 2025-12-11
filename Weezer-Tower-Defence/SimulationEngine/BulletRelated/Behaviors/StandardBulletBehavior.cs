using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.BulletRelated.Behaviors
{
    public class StandardBulletBehavior : IDamageDealerBehavior
    {
        private float _speed;
        private float _damage;

        public StandardBulletBehavior()
        {
            _speed = 10f;
            _damage = 20f;
        }

        public void Draw(DamageDealer damageDealer, SpriteBatch spriteBatch)
        {
            
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
