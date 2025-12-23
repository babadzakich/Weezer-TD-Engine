using Microsoft.Xna.Framework;
using System.Collections.Generic;
using SimulationEngine.EnemyRelated;
using SimulationEngine;

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
        var enemyController = GameManager.GetInstance().EnemyController;

        for (int i = damageDealers.Count - 1; i >= 0; i--)
        {
            var damageDealer = damageDealers[i];
            damageDealer.Update(deltaTime);

            if (!damageDealer.IsActive)
            {
                damageDealers.RemoveAt(i);
                continue;
            }

            if (enemyController == null)
                continue;

            // Simple collision check: bullet as point, enemy as small circle
            foreach (var enemy in enemyController.Enemies)
            {
                if (!enemy.isAlive)
                    continue;

                float distance = Vector2.Distance(damageDealer.Position, enemy.Position);
                const float hitRadius = 16f;

                if (distance <= hitRadius)
                {
                    enemy.TakeDamage(damageDealer.Behavior.Damage);

                    // Single-hit bullet: deactivate and remove
                    damageDealer.IsActive = false;
                    damageDealers.RemoveAt(i);
                    break;
                }
            }
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