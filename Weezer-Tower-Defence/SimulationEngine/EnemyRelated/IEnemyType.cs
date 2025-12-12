using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.EnemyRelated
{
    public interface IEnemyType
    {
        void TakeDamage(float amount);
        public void Update(Enemy enemy, GameTime gameTime, MapRelated.Path path);
        public void Draw(Enemy enemy, SpriteBatch spriteBatch);
    }
}