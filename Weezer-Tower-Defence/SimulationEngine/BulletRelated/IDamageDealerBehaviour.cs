using Microsoft.Xna.Framework;

namespace SimulationEngine.BulletRelated
{
    public interface IDamageDealerBehavior
    {
        void Update(DamageDealer damageDealer, GameTime gameTime);
    }
}
