using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.BulletRelated;

public class DamageDealer
{
    public IDamageDealerBehavior Behavior { get; }
    public Texture2D Texture { get; set; }
    public float HitRadius { get; }
    public string OwnerInstanceId { get; set; } = string.Empty;

    public Vector2 position;
    public bool IsActive = true;
    public Vector2 Direction;
    public float Rotation;
    public readonly DamageDealerController controller = DamageDealerController.GetInstance(null);
    public Vector2 StartPosition { get; }

    public DamageDealer(IDamageDealerBehavior behavior, Vector2 startPosition, Vector2 direction, float hitRadius)
    {
        Behavior = behavior;
        position = startPosition;
        StartPosition = startPosition;
        Direction = Vector2.Normalize(direction);
        Rotation = (float)System.Math.Atan2(Direction.Y, Direction.X);
        HitRadius = hitRadius;
    }


    public void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            Behavior.Update(this, gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            this.Behavior.Draw(this, spriteBatch);
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