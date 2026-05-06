using SimulationEngine.BulletRelated.Behaviors;

namespace SimulationEngine.BulletRelated.Behaviors;

public class TestBaseRoundBehavior : StandardBulletBehavior
{
    public TestBaseRoundBehavior(float damage, float speed, float maxDistance, float hitRadius = 5f)
        : base(damage, speed, maxDistance, hitRadius)
    {
    }
}
