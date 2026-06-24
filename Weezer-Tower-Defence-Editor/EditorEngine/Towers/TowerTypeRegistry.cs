using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EditorEngine.Towers;

/// <summary>
/// Реестр типов башен для редактора - загружает классы из EditorEngine.Towers.Types
/// </summary>
public class TowerTypeRegistry
{
    private static TowerTypeRegistry _instance;
    public static TowerTypeRegistry Instance => _instance ??= new TowerTypeRegistry();

    public class TowerTypeInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public Type Type { get; set; }
    }

    private Dictionary<string, TowerTypeInfo> towers = new();

    private TowerTypeRegistry()
    {
        LoadTowerTypes();
    }

    private void LoadTowerTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var towerTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && 
                   t.Namespace.StartsWith("EditorEngine.Towers.Types") &&
                   !t.IsInterface && 
                   !t.IsAbstract);

        foreach (var type in towerTypes)
        {
            try
            {
                // Создаём экземпляр и читаем его свойства
                var instance = Activator.CreateInstance(type);
                
                var idProp = type.GetProperty("Id");
                var nameProp = type.GetProperty("Name");
                var costProp = type.GetProperty("Cost");
                var rangeProp = type.GetProperty("Range");
                var fireRateProp = type.GetProperty("FireRate");

                if (idProp != null && nameProp != null)
                {
                    var info = new TowerTypeInfo
                    {
                        Id = idProp.GetValue(instance)?.ToString() ?? type.Name.ToLower(),
                        Name = nameProp.GetValue(instance)?.ToString() ?? type.Name,
                        Cost = (int)(costProp?.GetValue(instance) ?? 100),
                        Range = (float)(rangeProp?.GetValue(instance) ?? 100f),
                        FireRate = (float)(fireRateProp?.GetValue(instance) ?? 1f),
                        Type = type
                    };

                    towers[info.Id] = info;
                    Console.WriteLine($"Loaded tower type: {info.Name} (ID: {info.Id})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load tower type {type.Name}: {ex.Message}");
            }
        }
    }

    public List<TowerTypeInfo> GetAllTowerTypes() => towers.Values.ToList();
    public TowerTypeInfo GetTowerInfo(string id) => towers.GetValueOrDefault(id);
    public List<string> GetAllTowerTypeIds() => towers.Keys.ToList();
}
