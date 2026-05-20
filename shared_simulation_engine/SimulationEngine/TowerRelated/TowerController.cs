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

    public static void ResetInstance()
    {
        _instance = null;
    }

    public void AddTower(Tower tower)
    {
        towers.Add(tower);
    }

    public Tower FindTowerById(int id)
    {
        return towers.Find(t => t.Id == id);
    }

    public void RemoveTower(Tower tower)
    {
        towers.Remove(tower);
    }

    public void ClearTowers()
    {
        towers.Clear();
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