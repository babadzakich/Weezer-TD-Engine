using SimulationEngine.BulletRelated.Behaviors;

namespace SimulationEngine.BulletRelated.Behaviors;

public class TestRapidRoundBehavior : StandardBulletBehavior
{
    public TestRapidRoundBehavior(float damage, float speed, float maxDistance, float hitRadius = 4f)
        : base(damage, speed, maxDistance, hitRadius)
    {
    }
}
