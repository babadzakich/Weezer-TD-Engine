using System.Collections.Generic;
using Microsoft.Xna.Framework;

class EnemyManager : Controller
{
    private List<Enemy> enemies;
    private static EnemyManager _instance;

    private Game _engine;

    private EnemyManager(Game engine)
    {
        enemies = new List<Enemy>();
        _engine = engine;
    }

    public static EnemyManager GetInstance(Game engine)
    {
        if (_instance == null)
        {
            _instance = new EnemyManager(engine);
        }
        return _instance;
    }

    public void AddEnemy(Enemy enemy)
    {
        enemies.Add(enemy);
    }

    public void Update(double deltaTime)
    {
        foreach (var enemy in enemies)
        {
            enemy.Update(deltaTime, );
        }
    }

    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        foreach (var enemy in enemys)
        {
            enemy.Draw(spriteBatch);
        }
    }
}