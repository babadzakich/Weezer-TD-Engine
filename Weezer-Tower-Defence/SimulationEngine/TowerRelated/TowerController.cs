using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SimulationEngine.TowerRelated;

public class TowerController : Controller
{
    public readonly List<Tower> towers;
    private static TowerController _instance;

    private readonly Game _engine;
    
    private TowerController(Game engine)
    {
        towers = new List<Tower>();
        _engine = engine;
    }

    public static TowerController GetInstance(Game engine)
    {
        if (_instance == null)
        {
            _instance = new TowerController(engine);
        }
        return _instance;
    }

    public void AddTower(Tower tower)
    {
        towers.Add(tower);
    }

    public void Update(GameTime deltaTime)
    {
        foreach (var tower in towers)
        {
            tower.Update(deltaTime);
        }
    }

    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        foreach (var tower in towers)
        {
            tower.Draw(spriteBatch);
        }
    }
}