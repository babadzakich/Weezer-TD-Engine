using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.MapRelated;


namespace SimulationEngine.EnemyRelated;
public class EnemyController : Controller
{
    public List<Enemy> Enemies { get; }
    private static EnemyController _instance;

    private Game _engine;
    private GameMap _gameMap;
    private int _nextNetworkId = 1;
    private readonly Dictionary<int, Enemy> _enemiesById = new();

    public event Action<Enemy> OnEnemySpawned;
    public event Action<Enemy> OnEnemyKilled;
    public event Action<Enemy> OnEnemyReachedGoal;

    private EnemyController(Game engine, GameMap gameMap)
    {
        Enemies = new List<Enemy>();
        _engine = engine;
        _gameMap = gameMap;
    }

    public static EnemyController GetInstance(Game engine, GameMap gameMap = null)
    {
        if (_instance == null)
        {
            _instance = new EnemyController(engine, gameMap);
        }
        return _instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    public void AddEnemy(Enemy enemy)
    {
        if (enemy.NetworkId < 0)
            enemy.NetworkId = _nextNetworkId++;
        _enemiesById[enemy.NetworkId] = enemy;
        Enemies.Add(enemy);
        OnEnemySpawned?.Invoke(enemy);
    }

    public Enemy GetByNetworkId(int networkId)
        => _enemiesById.TryGetValue(networkId, out var e) ? e : null;

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy.NetworkId >= 0)
            _enemiesById.Remove(enemy.NetworkId);
        Enemies.Remove(enemy);
    }

    public void Update(GameTime gameTime)
    {
        for (int i = Enemies.Count - 1; i >= 0; i--)
        {
            var enemy = Enemies[i];
            if (enemy.isAlive)
            {
                enemy.Update(gameTime);
            }
            else
            {
                if (!enemy.isKilled)
                {
                    // Enemy reached defense point
                    if (_gameMap != null)
                    {
                        var defensePoint = _gameMap.GetDefensePoint(enemy.GetDefensePointId());
                        defensePoint?.TakeDamage(enemy.Damage);
                    }
                    OnEnemyReachedGoal?.Invoke(enemy);
                }
                else
                {
                    OnEnemyKilled?.Invoke(enemy);
                }

                if (enemy.NetworkId >= 0)
                    _enemiesById.Remove(enemy.NetworkId);
                Enemies.RemoveAt(i);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var enemy in Enemies)
        {
            if (enemy.isAlive)
                enemy.Draw(spriteBatch);
        }
    }

    /// <summary>
    /// Approximate distance from an enemy to its goal (defense point).
    /// Used for target priority: smaller distance => closer to goal.
    /// </summary>
    public float GetDistanceToGoal(Enemy enemy)
    {
        if (_gameMap == null || enemy == null)
            return float.MaxValue;

        var defensePoint = _gameMap.GetDefensePoint(enemy.GetDefensePointId());
        if (defensePoint == null)
            return float.MaxValue;

        return Vector2.Distance(enemy.Position, defensePoint.Position);
    }
}