using SimulationEngine.BulletRelated.Behaviors;

namespace SimulationEngine.BulletRelated.Behaviors;

public class TestHeavyShellBehavior : StandardBulletBehavior
{
    public TestHeavyShellBehavior(float damage, float speed, float maxDistance, float hitRadius = 8f)
        : base(damage, speed, maxDistance, hitRadius)
    {
    }
}
