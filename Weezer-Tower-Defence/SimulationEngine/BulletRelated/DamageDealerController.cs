using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace SimulationEngine.BulletRelated;

public class DamageDealerController : Controller
{
    public readonly List<DamageDealer> damageDealers;
    private static DamageDealerController _instance;

    private readonly Game _engine;

    private DamageDealerController(Game engine)
    {
        damageDealers = new List<DamageDealer>();
        _engine = engine;
    }

    public static DamageDealerController GetInstance(Game engine)
    {
        if (_instance == null)
        {
            _instance = new DamageDealerController(engine);
        }
        return _instance;
    }

    public void AddDamageDealer(DamageDealer damageDealer)
    {
        damageDealers.Add(damageDealer);
    }

    public void RemoveDamageDealer(DamageDealer damageDealer)
    {
        damageDealers.Remove(damageDealer);
    }

    public void Update(GameTime deltaTime)
    {
        foreach (var damageDealer in damageDealers)
        {
            damageDealer.Update(deltaTime);
        }
    }

    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        foreach (var damageDealer in damageDealers)
        {
            damageDealer.Draw(spriteBatch);
        }
    }
}