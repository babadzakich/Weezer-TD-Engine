class TowerType
{
    public string Id { get; } // уникальный идентификатор типа башни
    public int Cost { get; }
    public float Range { get; } // радиус обнаружения в пикселях
    public float FireRate { get; } // выстрелов в секунду
    // public DamageDealer Projectile { get; }
    public string SpriteName { get; } // имя текстуры в Content


    public TowerType() { }
}