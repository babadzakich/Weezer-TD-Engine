using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

class Enemy
{
    public Vector2 Position { get; set; }
    private EnemyType _type;
    // private List<Enemy> enemiesInRange;
    //private float _attackCooldown;
    private float _moveCooldown;

    private Texture2D _sprite;

    public Enemy(EnemyType enemyType, Vector2 position)
    {
        _type = type;
        Position = position;
    }

    /**
    * Move creature on map
    */
    public void Update(GameTime gameTime)
    {

        _moveCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_moveCooldown <= 0f)
        {
            move();
        }
    }
    public void Draw(SpriteBatch sb)
    {
        ///
    }

    private void move()
    {

    }
}