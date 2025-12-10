using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.BulletRelated;

public class DamageDealer
{
    public DamageDealerConfig Config { get; }
    public IDamageDealerBehavior Behavior { get; }
    public Texture2D Texture { get; set; }

    public Vector2 position;
    public bool IsActive = true;
    public Vector2 Direction;
    public float Rotation;
    public readonly DamageDealerController controller = DamageDealerController.GetInstance(null);

    public DamageDealer(DamageDealerConfig config, IDamageDealerBehavior behavior, Vector2 startPosition, Vector2 direction)
    {
        Config = config;
        Behavior = behavior;
        position = startPosition;
        Direction = Vector2.Normalize(direction);
        Rotation = (float)System.Math.Atan2(Direction.Y, Direction.X);
    }


    public void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            Behavior.Update(this, gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive || Texture == null) return;

            Vector2 origin = new Vector2(Texture.Width / 2f, Texture.Height / 2f);
            spriteBatch.Draw(Texture, Position, null, Config.TintColor, Rotation, origin, Config.Scale, SpriteEffects.None, 0f);
        }
    
    public Vector2 Position
    {
        get => position;
        set => position = value;
    }

    public bool IsOutOfBounds(Rectangle bounds)
    {
        return Position.X < bounds.Left || Position.X > bounds.Right ||
               Position.Y < bounds.Top || Position.Y > bounds.Bottom;
    }
}