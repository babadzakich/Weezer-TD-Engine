# Система врагов в редакторе

## Концепция

Система разделена на **Поведения** (Behaviors) и **Конфиги** (Configs):

### Поведения (IEnemyBehavior)

**Где:** `EditorEngine/Enemies/Behaviors/`

Определяют КАК враг движется, атакует, рендерится. Это код, логика.

**Примеры:**

- `StandardEnemyBehavior` - просто идёт по пути
- `FlyingEnemyBehavior` - летит прямо к цели, быстрее
- `TeleportingEnemyBehavior` - телепортируется вперёд каждые 2 секунды

### Конфиги (EnemyConfig)

**Где:** `EditorEngine/Enemies/Configs/*.json`

Определяют конкретных врагов с их параметрами. Это данные.

**Пример:** `goblin.json`

```json
{
  "Id": "goblin",
  "DisplayName": "Goblin",
  "BehaviorId": "standard",
  "BaseHealth": 100,
  "BaseSpeed": 60.0,
  "Damage": 10
}
```

## Как создать нового врага

### 1. Выбери существующее поведение

Посмотри в `EditorEngine/Enemies/Behaviors/`:

- `standard` - обычное движение
- `flying` - полёт
- `teleporting` - телепорт

### 2. Создай JSON конфиг

Создай файл `EditorEngine/Enemies/Configs/твой_враг.json`:

```json
{
  "Id": "boss_orc",
  "DisplayName": "Orc Boss",
  "BehaviorId": "standard",
  "BaseHealth": 500,
  "BaseSpeed": 30.0,
  "Damage": 50
}
```

Перезапусти редактор - враг появится в списке!

## Как создать новое поведение

Создай класс в `EditorEngine/Enemies/Behaviors/`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EditorEngine.Enemies.Behaviors;

public class ZigZagEnemyBehavior : IEnemyBehavior
{
    public string BehaviorId => "zigzag";
    public string BehaviorName => "ZigZag Movement";
    
    private float zigzagOffset = 0f;
    
    public void Update(EnemyInstance enemy, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Движение по пути
        enemy.PathProgress += enemy.Config.BaseSpeed * deltaTime;
        
        // Зигзаг влево-вправо
        zigzagOffset += deltaTime * 3f;
        float sideways = MathF.Sin(zigzagOffset) * 20f;
        // Применить к позиции (нужна доработка системы)
    }
    
    public void Draw(EnemyInstance enemy, SpriteBatch spriteBatch)
    {
        // Рисуем врага
    }
}
```

Теперь можешь использовать `"BehaviorId": "zigzag"` в конфигах!

## Примеры использования

**Слабый гоблин** (стандартное движение):
```json
{
  "Id": "goblin",
  "BehaviorId": "standard",
  "BaseHealth": 100,
  "BaseSpeed": 60.0,
  "Damage": 10
}
```

**Быстрый летающий дракон** (полёт):
```json
{
  "Id": "dragon",
  "BehaviorId": "flying",
  "BaseHealth": 200,
  "BaseSpeed": 100.0,
  "Damage": 30
}
```

**Призрак-телепортёр** (телепорт):
```json
{
  "Id": "ghost",
  "BehaviorId": "teleporting",
  "BaseHealth": 50,
  "BaseSpeed": 20.0,
  "Damage": 5
}
```

## Преимущества системы

1. **Одно поведение - много врагов**: `StandardEnemyBehavior` используется и для гоблинов (100 HP), и для орков (300 HP)
2. **Легко добавлять врагов**: просто создай JSON, не трогай код
3. **Легко добавлять поведения**: один класс - работает для всех врагов
4. **Комбинирование**: `FlyingEnemyBehavior` можно использовать для драконов, птиц, демонов - с разными параметрами

## Упаковка в уровень

При нажатии Ctrl+P создаётся:
```
Content/Levels/level_1/
  Maps/
    Enemies/
      goblin.json         ← конфиг врага
      orc.json
      StandardEnemyBehavior.cs  ← код поведения
      FlyingEnemyBehavior.cs
```
