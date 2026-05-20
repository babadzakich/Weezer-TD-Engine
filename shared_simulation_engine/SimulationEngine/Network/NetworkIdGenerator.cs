using System.Threading;

namespace SimulationEngine.Network;

internal static class NetworkIdGenerator
{
    private static int _nextEnemyId = 0;
    private static int _nextTowerId = 0;
    private static int _nextBulletId = 0;

    public static int NextEnemyId() => Interlocked.Increment(ref _nextEnemyId);
    public static int NextTowerId() => Interlocked.Increment(ref _nextTowerId);
    public static int NextBulletId() => Interlocked.Increment(ref _nextBulletId);
}
