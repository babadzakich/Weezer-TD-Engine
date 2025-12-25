using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimulationEngine.EnemyRelated;

namespace EditorEngine.Enemies;

/// <summary>
/// Реестр типов врагов для редактора - загружает реализации IEnemyType из папки EditorEngine/Enemies/Types/
/// </summary>
public class EnemyTypeRegistry
{
    private static EnemyTypeRegistry _instance;
    private Dictionary<string, EnemyTypeInfo> _enemyTypes;

    public class EnemyTypeInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public Type Type { get; set; }
        public int BaseHealth { get; set; }
        public float BaseSpeed { get; set; }
        public int Damage { get; set; }
    }

    private EnemyTypeRegistry()
    {
        _enemyTypes = new Dictionary<string, EnemyTypeInfo>();
        LoadEnemyTypes();
    }

    public static EnemyTypeRegistry Instance
    {
        get
        {
            if (_instance == null)
                _instance = new EnemyTypeRegistry();
            return _instance;
        }
    }

    /// <summary>
    /// Загружает классы врагов из namespace EditorEngine.Enemies.Types
    /// Просто создай новый .cs файл в папке EditorEngine/Enemies/Types/ с реализацией IEnemyType
    /// </summary>
    private void LoadEnemyTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var enemyTypes = assembly.GetTypes()
            .Where(t => typeof(IEnemyType).IsAssignableFrom(t) && 
                        !t.IsInterface && 
                        !t.IsAbstract &&
                        t.Namespace != null &&
                        t.Namespace.StartsWith("EditorEngine.Enemies.Types"));

        foreach (var type in enemyTypes)
        {
            try
            {
                var instance = Activator.CreateInstance(type) as IEnemyType;
                
                if (instance != null)
                {
                    string id = type.Name.Replace("EnemyType", "").Replace("Type", "").ToLower();
                    string displayName = FormatDisplayName(type.Name);
                    
                    var info = new EnemyTypeInfo
                    {
                        Id = id,
                        DisplayName = displayName,
                        Type = type,
                        BaseHealth = instance.health,
                        BaseSpeed = instance.speed,
                        Damage = instance.Damage
                    };
                    
                    _enemyTypes[id] = info;
                    Console.WriteLine($"Loaded enemy type: {displayName} (ID: {id})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load enemy type {type.Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Преобразует имя класса в читаемый формат
    /// </summary>
    private string FormatDisplayName(string typeName)
    {
        typeName = typeName.Replace("EnemyType", "").Replace("Type", "");
        
        string result = "";
        for (int i = 0; i < typeName.Length; i++)
        {
            if (i > 0 && char.IsUpper(typeName[i]))
                result += " ";
            result += typeName[i];
        }
        
        return result;
    }

    /// <summary>
    /// Получить информацию о типе врага
    /// </summary>
    public EnemyTypeInfo GetEnemyInfo(string enemyTypeId)
    {
        return _enemyTypes.TryGetValue(enemyTypeId.ToLower(), out var info) ? info : null;
    }

    /// <summary>
    /// Получить все зарегистрированные типы врагов
    /// </summary>
    public IReadOnlyList<EnemyTypeInfo> GetAllEnemyTypes()
    {
        return _enemyTypes.Values.ToList();
    }

    /// <summary>
    /// Получить список ID всех врагов
    /// </summary>
    public IReadOnlyList<string> GetAllEnemyTypeIds()
    {
        return _enemyTypes.Keys.ToList();
    }

    /// <summary>
    /// Проверить, зарегистрирован ли тип врага
    /// </summary>
    public bool IsRegistered(string enemyTypeId)
    {
        return _enemyTypes.ContainsKey(enemyTypeId.ToLower());
    }
}
