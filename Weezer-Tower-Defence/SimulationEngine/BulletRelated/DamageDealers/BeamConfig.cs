using Microsoft.Xna.Framework;

namespace SimulationEngine.BulletRelated.ProjectileConfig;

public class BeamConfig : DamageDealerConfig
{
    public float MaxRange { get; }

    public BeamConfig(int damageAmount, float speed, float maxRange = 10f)
        : base(damageAmount, speed, "Beam")
    {
        MaxRange = maxRange;
    }

    public override void ApplyBehavior(DamageDealer damageDealer, GameTime deltaTime)
    {
        
    }

    public static BeamConfig Default => new(damageAmount: 5, speed: 20f);
}