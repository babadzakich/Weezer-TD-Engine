using System;
using System.Collections.Generic;

namespace SimulationEngine.Persistence;

/// <summary>
/// Интерфейс для объектов, которые можно сохранять
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Сериализовать объект в словарь данных
    /// </summary>
    Dictionary<string, object> Serialize();
    
    /// <summary>
    /// Десериализовать объект из словаря данных
    /// </summary>
    void Deserialize(Dictionary<string, object> data);
}
