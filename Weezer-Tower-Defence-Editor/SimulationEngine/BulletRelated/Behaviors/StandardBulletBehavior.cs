using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using SimulationEngine.BulletRelated;

public class StandardBulletBehavior : IDamageDealerBehavior
{
    private float _speed;
    private float _damage;
    private float _maxDistance;
    private float _hitRadius;

    public StandardBulletBehavior(float damage, float speed, float maxDistance, float hitRadius = 5f)
    {
        _damage = damage;
        _speed = speed;
        _maxDistance = maxDistance;
        _hitRadius = hitRadius;
    }

    public float Damage 
    { 
        get => _damage; 
        set => _damage = value; 
    }

    public float HitRadius 
    { 
        get => _hitRadius; 
        set => _hitRadius = value; 
    }

    public float Speed 
    { 
        get => _speed; 
        set => _speed = value; 
    }

    public void Draw(DamageDealer damageDealer, SpriteBatch spriteBatch)
    {
        // Используем текстуру и HitRadius из самой пули
        // Если текстура не создана, создаём её на основе HitRadius пули
        if (damageDealer.Texture == null)
        {
            CreateBulletTexture(damageDealer, spriteBatch);
        }
        
        // Рисуем пулю как круг белого цвета, центрированный по позиции
        if (damageDealer.Texture != null)
        {
            spriteBatch.Draw(damageDealer.Texture, damageDealer.Position, null, Color.White, 0f,
                new Vector2(damageDealer.Texture.Width / 2f, damageDealer.Texture.Height / 2f), 1f, SpriteEffects.None, 0f);
        }
    }

    private void CreateBulletTexture(DamageDealer damageDealer, SpriteBatch spriteBatch)
    {
        // Создаём текстуру круга, размер которой точно соответствует HitRadius пули
        // Размер текстуры = диаметр круга (радиус * 2), округляем вверх для точности
        int textureSize = (int)System.Math.Ceiling(damageDealer.HitRadius * 2);
        if (textureSize < 1) textureSize = 1; // Минимальный размер
                
        damageDealer.Texture = new Texture2D(spriteBatch.GraphicsDevice, textureSize, textureSize);
        Color[] bulletData = new Color[textureSize * textureSize];
        
        // Заполняем текстуру: белый цвет внутри круга, прозрачный снаружи
        // Центр текстуры точно в середине
        float centerX = (textureSize - 1) / 2f;
        float centerY = (textureSize - 1) / 2f;
        float radius = damageDealer.HitRadius;
        float radiusSquared = radius * radius; // Используем квадрат радиуса для оптимизации
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distanceSquared = dx * dx + dy * dy;
                
                // Если точка внутри или на границе круга - белый цвет
                // Используем <= для точного соответствия хитбоксу (который использует <=)
                if (distanceSquared <= radiusSquared)
                {
                    bulletData[y * textureSize + x] = Color.White;
                }
                else
                {
                    bulletData[y * textureSize + x] = Color.Transparent;
                }
            }
        }
        
        damageDealer.Texture.SetData(bulletData);
    }

    public void Update(DamageDealer bullet, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        bullet.Position += bullet.Direction * _speed * deltaTime;

        // Deactivate if bullet travelled beyond its maximum distance
        float traveled = Vector2.Distance(bullet.StartPosition, bullet.Position);
        if (traveled >= _maxDistance)
        {
            bullet.IsActive = false;
        }
    }

    private void onHitTarget(DamageDealer bullet/*, EnemyController targets*/)
    {
        // Logic for when the bullet hits a target
        // e.g., apply damage, play effects, deactivate bullet, etc.
        bullet.IsActive = false;
    }
}

