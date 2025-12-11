using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimulationEngine.TowerRelated.Behaviors;

/// <summary>
/// Пример кастомной башни для плагина
/// Чтобы создать свою башню, нужно:
/// 1. Создать класс, реализующий ITowerBehavior
/// 2. Реализовать все свойства (Id, Name, Cost, Range, FireRate, Damage)
/// 3. Реализовать методы Update, FindTarget, Fire, Draw
/// 4. Зарегистрировать в TowerBehaviorRegistry
/// </summary>
public class ExampleCustomTower : ITowerBehavior
{
    // ОБЯЗАТЕЛЬНЫЕ СВОЙСТВА - определяют параметры башни
    public string Id => "example_custom";
    public string Name => "Example Custom Tower";
    public int Cost => 150;
    public float Range => 200f;
    public float FireRate => 2f;
    
    // Можно добавить свои поля для внутренней логики
    private float _cooldown;
    private Vector2? _lastTarget;

    public ExampleCustomTower()
    {
        _cooldown = 0;
    }

    /// <summary>
    /// Обновление логики башни каждый кадр
    /// </summary>
    public void Update(Tower tower, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _cooldown -= deltaTime;
        
        // Ваша логика обновления
        // Например: поиск целей, стрельба, эффекты и т.д.
        
        if (_cooldown <= 0)
        {
            var target = FindTarget(tower, new object[0]); // TODO: передать реальных врагов
            if (target.HasValue)
            {
                Fire(tower, target.Value);
                _cooldown = 1f / FireRate;
            }
        }
    }

    /// <summary>
    /// Найти цель для атаки
    /// </summary>
    public Vector2? FindTarget(Tower tower, object[] enemies)
    {
        // Ваша логика поиска цели
        // Например: ближайший враг, самый сильный, самый слабый и т.д.
        
        // Для примера возвращаем null (нет цели)
        return null;
    }

    /// <summary>
    /// Выстрелить по цели
    /// </summary>
    public void Fire(Tower tower, Vector2 targetPosition)
    {
        // Ваша логика стрельбы
        // Например: создать пулю, эффект, нанести урон напрямую и т.д.
        
        Vector2 direction = Vector2.Normalize(targetPosition - tower.Position);
        
        // Пример: создать пулю
        // var bullet = new DamageDealer(new StandardBulletBehavior(), tower.Position, direction);
        // DamageDealerController.GetInstance(null).AddDamageDealer(bullet);
    }

    /// <summary>
    /// Отрисовка башни
    /// </summary>
    public void Draw(Tower tower, SpriteBatch spriteBatch, Texture2D texture)
    {
        // Базовая отрисовка - просто рисуем текстуру
        if (texture != null)
        {
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            spriteBatch.Draw(texture, tower.Position, null, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
        
        // Можно добавить кастомную отрисовку:
        // - эффекты
        // - анимацию
        // - радиус действия
        // - цель и т.д.
    }
}

// Как зарегистрировать эту башню:
// TowerBehaviorRegistry.Instance.Register("example_custom", () => new ExampleCustomTower());
