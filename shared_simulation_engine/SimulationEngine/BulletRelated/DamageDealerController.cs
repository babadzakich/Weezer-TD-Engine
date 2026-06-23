using System;
using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using SimulationEngine.EnemyRelated;
using SimulationEngine;

namespace SimulationEngine.BulletRelated;
public class DamageDealerController : Controller
{
    public List<DamageDealer> DamageDealers => damageDealers;
    public readonly List<DamageDealer> damageDealers;
    private readonly List<Infrastructure.VisualEffect> _activeEffects = new();
    private static DamageDealerController _instance;
    private readonly Dictionary<string, Microsoft.Xna.Framework.Graphics.Texture2D> _textureCache = new();
    private readonly Dictionary<string, Microsoft.Xna.Framework.Graphics.Texture2D> _explosionCache = new();
    public Microsoft.Xna.Framework.Graphics.Texture2D DefaultTexture { get; set; }

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

    public static void ResetInstance()
    {
        _instance = null;
    }

    public void AddDamageDealer(DamageDealer damageDealer)
    {
        if (damageDealer.Texture == null || damageDealer.Texture == DefaultTexture)
        {
            damageDealer.Texture = GetBulletTexture(damageDealer);
        }
        damageDealers.Add(damageDealer);
    }

    public Microsoft.Xna.Framework.Graphics.Texture2D GetBulletTexture(DamageDealer damageDealer)
    {
        if (damageDealer?.Behavior == null) return DefaultTexture;

        // Используем имя класса поведения как ключ для поиска спрайта (например, TestBaseRoundBehavior)
        string className = damageDealer.Behavior.GetType().Name;
        
        // Пытаемся найти в кэше
        if (_textureCache.TryGetValue(className, out var cached)) return cached;

        // Ищем файл. Мы проверяем два варианта: полное имя класса и имя в нижнем регистре (более привычно для ID)
        // Также проверяем имя без "Behavior"
        string shortName = className.Replace("Behavior", "");
        string[] possibleNames = { className, shortName, shortName.ToLower() };
        string baseDir = Infrastructure.PathService.GetEntityDllDirectory("damageDealers");

        foreach (var name in possibleNames)
        {
            string customPath = System.IO.Path.Combine(baseDir, $"{name}.png");
            if (System.IO.File.Exists(customPath))
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(customPath))
                    {
                        var texture = Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(_engine.GraphicsDevice, stream);
                        _textureCache[className] = texture;
                        Console.WriteLine($"Loaded unique projectile sprite: {customPath}");
                        return texture;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading unique projectile sprite {name}: {ex.Message}");
                }
            }
        }

        return DefaultTexture;
    }

    public void RemoveDamageDealer(DamageDealer damageDealer)
    {
        damageDealers.Remove(damageDealer);
    }

    public void Update(GameTime deltaTime)
    {
        var enemyController = GameManager.GetInstance().EnemyController;

        // Обновление эффектов
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].Update(deltaTime);
            if (!_activeEffects[i].IsActive) _activeEffects.RemoveAt(i);
        }

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

            // Collision check: circle-circle intersection
            foreach (var enemy in enemyController.Enemies)
            {
                if (!enemy.isAlive)
                    continue;

                float distance = Vector2.Distance(damageDealer.Position, enemy.Position);
                float bulletRadius = damageDealer.HitRadius;
                float enemyRadius = enemy.HitRadius;
                float combinedRadius = bulletRadius + enemyRadius;

                if (distance <= combinedRadius)
                {
                    enemy.TakeDamage(damageDealer.Behavior.Damage, damageDealer.OwnerInstanceId);
                    
                    // Спавним взрыв
                    SpawnExplosion(damageDealer);

                    // Single-hit bullet: deactivate and remove
                    damageDealer.IsActive = false;
                    damageDealers.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void SpawnExplosion(DamageDealer bullet)
    {
        if (bullet?.Behavior == null) return;
        
        string className = bullet.Behavior.GetType().Name;
        string shortName = className.Replace("Behavior", "");
        
        if (!_explosionCache.TryGetValue(shortName, out var explosionTexture))
        {
            // Пытаемся найти текстуру [Name]_explosion.png
            string baseDir = Infrastructure.PathService.GetEntityDllDirectory("damageDealers");
            string path = System.IO.Path.Combine(baseDir, $"{shortName}_explosion.png");
            
            if (System.IO.File.Exists(path))
            {
                try {
                    using (var stream = System.IO.File.OpenRead(path)) {
                        explosionTexture = Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(_engine.GraphicsDevice, stream);
                        _explosionCache[shortName] = explosionTexture;
                    }
                } catch { }
            }
        }

        if (explosionTexture != null)
        {
            // Создаем эффект: 5 кадров (в ряд), длительность 0.3 сек, масштаб по радиусу пули
            var effect = new Infrastructure.VisualEffect(explosionTexture, bullet.Position, 5, 0.3f, bullet.HitRadius / 5f);
            _activeEffects.Add(effect);
        }
    }

    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        foreach (var damageDealer in damageDealers)
        {
            damageDealer.Draw(spriteBatch);
        }

        foreach (var effect in _activeEffects)
        {
            effect.Draw(spriteBatch);
        }
    }
}