using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.MapRelated;
using GamePath = SimulationEngine.MapRelated.Path;

namespace SimulationEngine.EnemyRelated;

/// <summary>
/// Фабрика для создания врагов по строковым ID из загруженных типов
/// </summary>
public class EnemyTypeFactory
{
    private static EnemyTypeFactory _instance;
    private Dictionary<string, Type> _enemyTypes = new();
    private Dictionary<string, LevelLoader.EnemyDefinition> _enemyDefinitions = new();

    public static EnemyTypeFactory Instance
    {
        get
        {
            if (_instance == null)
                _instance = new EnemyTypeFactory();
            return _instance;
        }
    }

    private EnemyTypeFactory()
    {
        // Загружаем встроенные типы врагов из SimulationEngine
        LoadBuiltInEnemyTypes();
    }

    private void LoadBuiltInEnemyTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var enemyTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEnemyType).IsAssignableFrom(t));

        foreach (var type in enemyTypes)
        {
            // Используем имя класса без суффикса "Type" как ID
            string typeName = type.Name.Replace("EnemyType", "").ToLower();
            _enemyTypes[typeName] = type;
            Console.WriteLine($"Registered built-in enemy type: {typeName} -> {type.Name}");
        }
    }

    /// <summary>
    /// Загружает типы врагов из скомпилированных .cs файлов уровня
    /// </summary>
    public void LoadEnemyTypesFromLevel(Dictionary<string, LevelLoader.EnemyDefinition> enemyDefinitions)
    {
        _enemyDefinitions = enemyDefinitions;
        
        foreach (var def in enemyDefinitions.Values)
        {
            Console.WriteLine($"Registered enemy definition: {def.Id} (behavior: {def.BehaviorId})");
        }
    }

    /// <summary>
    /// Создать врага по строковому ID
    /// </summary>
    public Enemy CreateEnemy(string enemyId, Vector2 position, GamePath path)
    {
        // Проверяем, есть ли определение врага
        if (!_enemyDefinitions.TryGetValue(enemyId, out var definition))
        {
            Console.WriteLine($"Error: Enemy definition not found for {enemyId}");
            return null;
        }

        // Пытаемся найти тип по BehaviorId
        Type enemyType = null;
        
        // Сначала ищем в зарегистрированных типах по behavior ID
        if (_enemyTypes.TryGetValue(definition.BehaviorId.ToLower(), out var behaviorType))
        {
            enemyType = behaviorType;
        }
        // Если не нашли, пробуем по ID врага
        else if (_enemyTypes.TryGetValue(enemyId.ToLower(), out var idType))
        {
            enemyType = idType;
        }
        // Если не нашли, используем базовый тип
        else
        {
            Console.WriteLine($"Warning: No specific type found for {enemyId}, using BasicEnemyType");
            enemyType = typeof(EnemyTypes.BasicEnemyType);
        }

        // Создаём экземпляр IEnemyType
        IEnemyType enemyTypeInstance = null;
        
        // Пытаемся создать через конструктор с Texture2D
        var textureConstructor = enemyType.GetConstructor(new[] { typeof(Texture2D) });
        if (textureConstructor != null)
        {
            enemyTypeInstance = (IEnemyType)textureConstructor.Invoke(new object[] { null });
        }
        else
        {
            // Пробуем конструктор без параметров
            var defaultConstructor = enemyType.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor != null)
            {
                enemyTypeInstance = (IEnemyType)defaultConstructor.Invoke(null);
            }
        }
        
        if (enemyTypeInstance == null)
        {
            Console.WriteLine($"ERROR: Could not create instance of {enemyType.Name}");
            return null;
        }
        
        // TODO: Применить кастомные параметры из definition (BaseHealth, BaseSpeed)
        // Сейчас используются дефолтные значения из типа врага
        
        // Создаём врага
        var enemy = new Enemy(enemyTypeInstance, position, path);

        Console.WriteLine($"Created enemy: {enemyId} at {position} (using default type parameters)");
        
        return enemy;
    }

    /// <summary>
    /// Проверить, существует ли враг с данным ID
    /// </summary>
    public bool HasEnemyDefinition(string enemyId)
    {
        return _enemyDefinitions.ContainsKey(enemyId);
    }

    /// <summary>
    /// Получить определение врага
    /// </summary>
    public LevelLoader.EnemyDefinition GetEnemyDefinition(string enemyId)
    {
        return _enemyDefinitions.GetValueOrDefault(enemyId);
    }
}
