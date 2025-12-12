using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.EnemyRelated
{
    public interface IEnemyType
    {
        public int health { get; }
        public float speed { get; }
        public int Damage { get; } // Урон, который враг наносит базе
        void TakeDamage(float amount);
        public void Update(Enemy enemy, GameTime gameTime, MapRelated.Path path);
        public void Draw(Enemy enemy, SpriteBatch spriteBatch);
    }
}