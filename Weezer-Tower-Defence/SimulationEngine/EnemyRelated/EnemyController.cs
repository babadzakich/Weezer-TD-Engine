using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine;

public class EnemyController : Controller
{
    public List<Enemy> Enemies { get; }
    private static EnemyController _instance;

    private Game _engine;

    private EnemyController(Game engine)
    {
        Enemies = new List<Enemy>();
        _engine = engine;
    }

    public static EnemyController GetInstance(Game engine)
    {
        if (_instance == null)
        {
            _instance = new EnemyController(engine);
        }
        return _instance;
    }

    public void AddEnemy(Enemy enemy)
    {
        Enemies.Add(enemy);
    }

    public void Update(GameTime gameTime)
    {
        foreach (var enemy in Enemies)
        {
            enemy.Update(gameTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var enemy in Enemies)
        {
            enemy.Draw(spriteBatch);
        }
    }
}