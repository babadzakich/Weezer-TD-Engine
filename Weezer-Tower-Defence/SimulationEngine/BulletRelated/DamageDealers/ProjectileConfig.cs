using Microsoft.Xna.Framework;

namespace SimulationEngine.BulletRelated.ProjectileConfig;

public class ProjectileConfig : DamageDealerConfig
{
    public ProjectileConfig(int damageAmount, float speed)
        : base(damageAmount, speed, "Projectile")
    {
    }

    public override void ApplyBehavior(DamageDealer damageDealer, GameTime deltaTime)
    {
        damageDealer.Position += damageDealer.Direction * Speed * (float)deltaTime.ElapsedGameTime.TotalSeconds;
    }

    public static ProjectileConfig Default => new(damageAmount: 10, speed: 5f);
}