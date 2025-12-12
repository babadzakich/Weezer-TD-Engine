using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.EnemyRelated;

public class EnemyController : Controller
{
    public List<Enemy> Enemies { get; }
    private static EnemyController _instance;

    private Game _engine;
    private MapRelated.GameMap _gameMap;

    private EnemyController(Game engine, MapRelated.GameMap gameMap)
    {
        Enemies = new List<Enemy>();
        _engine = engine;
        _gameMap = gameMap;
    }

    public static EnemyController GetInstance(Game engine, MapRelated.GameMap gameMap = null)
    {
        if (_instance == null)
        {
            _instance = new EnemyController(engine, gameMap);
        }
        return _instance;
    }

    public void AddEnemy(Enemy enemy)
    {
        Enemies.Add(enemy);
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
                // Враг достиг конца пути - наносим урон базе
                if (_gameMap != null)
                {
                    var defensePoint = _gameMap.GetDefensePoint(enemy.GetDefensePointId());
                    if (defensePoint != null)
                    {
                        defensePoint.TakeDamage(enemy.Damage);
                    }
                }
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
}