using Microsoft.Xna.Framework;

namespace SimulationEngine.BulletRelated.Behaviors
{
    public class StandardBulletBehavior : IDamageDealerBehavior
    {
        public void Update(DamageDealer bullet, GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            bullet.Position += bullet.Direction * bullet.Config.Speed * deltaTime;
            
            // Basic bounds check or logic could go here, 
            // but usually the controller handles removal if out of bounds.
            // For now, just movement.
        }
    }
}
