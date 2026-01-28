using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.EnemyRelated
{
    public interface IEnemyType
    {
        public int health { get; set; }
        public int MaxHealth { get; set; }
        public float speed { get; set; }
        public int Damage { get; set; } // Урон, который враг наносит базе
        public float HitRadius { get; set; } // Радиус хитбокса врага
        void TakeDamage(float amount);
        public void Update(Enemy enemy, GameTime gameTime, MapRelated.Path path);
        public void Draw(Enemy enemy, SpriteBatch spriteBatch);
    }
}