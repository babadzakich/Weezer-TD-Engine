using Microsoft.Xna.Framework;
using SimulationEngine.BulletRelated.DamageDealers;
using SimulationEngine.TowerRelated;

public class SniperTowerConfig : TowerConfig
{
    public SniperTowerConfig() 
        : base("sniper", cost: 500, range: 300f, fireRate: 0.5f,
               projectileConfig: new ProjectileConfig(10, 15f),
               texturePath: "Towers/Sniper",
               tintColor: Color.DarkBlue)
    { }
    
//     public override Vector2? FindTarget(Tower tower, object[] enemies)
//     {
//         // Снайпер целится в самого далёкого врага
//     }
    
    public static SniperTowerConfig Default => new();
}