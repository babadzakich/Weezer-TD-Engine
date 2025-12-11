using Microsoft.Xna.Framework;

namespace SimulationEngine.BulletRelated
{
    public interface IDamageDealerBehavior
    {
        void Update(DamageDealer damageDealer, GameTime gameTime);
        void Draw(DamageDealer damageDealer, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch);
    }
}
