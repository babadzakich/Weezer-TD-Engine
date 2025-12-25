using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EditorEngine.DamageDealers;

/// <summary>
/// Реестр типов damage dealers для редактора - загружает классы из EditorEngine.DamageDealers.Types
/// </summary>
public class DamageDealerTypeRegistry
{
    private static DamageDealerTypeRegistry? _instance;
    public static DamageDealerTypeRegistry Instance => _instance ??= new DamageDealerTypeRegistry();

    public class DamageDealerTypeInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Type Type { get; set; }
    }

    private Dictionary<string, DamageDealerTypeInfo> damageDealers = new();

    private DamageDealerTypeRegistry()
    {
        LoadDamageDealerTypes();
    }

    private void LoadDamageDealerTypes()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var ddTypes = assembly.GetTypes()
            .Where(t => t.Namespace != null && 
                   t.Namespace.StartsWith("EditorEngine.DamageDealers.Types") &&
                   !t.IsInterface && 
                   !t.IsAbstract);

        foreach (var type in ddTypes)
        {
            try
            {
                var info = new DamageDealerTypeInfo
                {
                    Id = type.Name.ToLower().Replace("behavior", ""),
                    Name = type.Name,
                    Type = type
                };

                damageDealers[info.Id] = info;
                Console.WriteLine($"Loaded damage dealer type: {info.Name} (ID: {info.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load damage dealer type {type.Name}: {ex.Message}");
            }
        }
    }

    public List<DamageDealerTypeInfo> GetAllDamageDealerTypes() => damageDealers.Values.ToList();
    public DamageDealerTypeInfo? GetDamageDealerInfo(string id) => damageDealers.GetValueOrDefault(id);
    public List<string> GetAllDamageDealerTypeIds() => damageDealers.Keys.ToList();
}
