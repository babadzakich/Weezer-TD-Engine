using System.Collections.Generic;

class Tower
{
    TowerType type;

    // List<Enemy> enemiesInRange;
    float fireCooldown;

    public Tower(TowerType type)
    {
        this.type = type;
        // enemiesInRange = new List<Enemy>();
        fireCooldown = 0f;
    }

    public void Update(float deltaTime)
    {
        if (fireCooldown > 0)
        {
            fireCooldown -= deltaTime;
        }
    }
}