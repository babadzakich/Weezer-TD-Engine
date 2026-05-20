using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.Network;

namespace SimulationEngine.BulletRelated;

public class DamageDealer
{
    public int Id { get; set; }
    public string BehaviorId { get; set; } = string.Empty;
    public int? TargetId { get; set; }
    public IDamageDealerBehavior Behavior { get; }
    public Texture2D Texture { get; set; }
    public float HitRadius { get; }

    public Vector2 position;
    public bool IsActive = true;
    public Vector2 Direction;
    public float Rotation;
    public readonly DamageDealerController controller = DamageDealerController.GetInstance(null);
    public Vector2 StartPosition { get; }
    public float Elapsed { get; set; }

    public DamageDealer(IDamageDealerBehavior behavior, Vector2 startPosition, Vector2 direction, float hitRadius)
    {
        Id = NetworkIdGenerator.NextBulletId();
        BehaviorId = behavior.GetType().Name;
        Behavior = behavior;
        position = startPosition;
        StartPosition = startPosition;
        Direction = Vector2.Normalize(direction);
        Rotation = (float)System.Math.Atan2(Direction.Y, Direction.X);
        HitRadius = hitRadius;
        Elapsed = 0f;
    }


    public void Update(GameTime gameTime)
    {
        if (!IsActive) return;
        Behavior.Update(this, gameTime);
        Elapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
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