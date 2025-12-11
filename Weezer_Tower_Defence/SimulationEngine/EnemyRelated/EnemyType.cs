class EnemyType
{
    public string Id { get; } // идентификатор типа кричи
    public float Hp { get; } // Hit points
    public float Size { get; } // радиус круга существа в пикселях (в некотором смысле хит-бокс?)
    public float Damage { get; } // Урон от того что он дошел до конца
    public float Speed { get; } // Скорость передвижения (в секунду ?)
    // public DamageDealer Projectile { get; }
    public string SpriteName { get; } // имя текстуры в Content


    public EnemyType() { }
}