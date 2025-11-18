using System.Collections.Generic;
using Microsoft.Xna.Framework;

class TowerManager : Manager
{
    private List<Tower> towers;
    private static TowerManager _instance;

    private Game _engine;

    private TowerManager(Game engine)
    {
        towers = new List<Tower>();
        _engine = engine;
    }

    public static TowerManager GetInstance(Game engine)
    {
        if (_instance == null)
        {
            _instance = new TowerManager(engine);
        }
        return _instance;
    }

    public void AddTower(Tower tower)
    {
        towers.Add(tower);
    }

    public void Update(double deltaTime)
    {
        foreach (var tower in towers)
        {
            tower.Update(deltaTime, );
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