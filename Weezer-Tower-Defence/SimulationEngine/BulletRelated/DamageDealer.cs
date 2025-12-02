using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.BulletRelated;

public class DamageDealer
{
    public DamageDealerConfig Config { get; }
    public Texture2D Texture { get; set; }

    private Vector2 position;
    private Vector2 direction;
    private float rotation;
    private readonly DamageDealerController controller = DamageDealerController.GetInstance(null);

    public DamageDealer(DamageDealerConfig config, Vector2 startPosition, Vector2 direction)
    {
        Config = config;
        position = startPosition;
        this.direction = Vector2.Normalize(direction);
        rotation = (float)System.Math.Atan2(this.direction.Y, this.direction.X);
    }


    public void Update(GameTime deltaTime)
    {
        Config.ApplyBehavior(this, deltaTime);

        if (position.X < 0 || position.Y < 0)
        {
            controller.RemoveDamageDealer(this);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (Texture != null)
        {
            Config.Draw(spriteBatch, Texture, position, rotation);
        }
    }
    
    public Vector2 Position
    {
        get => position;
        set => position = value;
    }
    
    public Vector2 Direction
    {
        get => direction;
        set => direction = value;
    }
    
    public float Rotation
    {
        get => rotation;
        set => rotation = value;
    }
}