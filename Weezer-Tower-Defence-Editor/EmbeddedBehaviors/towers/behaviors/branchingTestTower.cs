using SimulationEngine.BulletRelated;

namespace SimulationEngine.TowerRelated.Behaviors;

public class BranchingTestTowerBehavior : BasicTowerBehavior
{
    public BranchingTestTowerBehavior(
        string id,
        string name,
        IDamageDealerBehavior projectileConfig,
        int cost,
        float range,
        float fireRate)
        : base(id, name, projectileConfig, cost, range, fireRate)
    {
    }
}
