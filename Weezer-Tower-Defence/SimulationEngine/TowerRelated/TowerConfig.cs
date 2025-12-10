using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace SimulationEngine.TowerRelated;

/// <summary>
/// Базовый класс типа башни. Наследуйте от него для создания своих башен в плагинах.
/// </summary>
public class TowerConfig
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Cost { get; set; }
    public float Range { get; set; }
    public float FireRate { get; set; }
    public float Damage { get; set; }
    
    // Визуальные данные
    public string TexturePath { get; set; }
    public string TintColorHex { get; set; } // Цвет в формате #RRGGBBAA
    public float Scale { get; set; }
    
    // Данные о снаряде
    public string ProjectileConfigId { get; set; }
    
    // Стратегия поведения (имя класса)
    public string BehaviorType { get; set; } // Например: "BasicTowerBehavior", "SniperTowerBehavior"
    
    // Дополнительные параметры (гибкие данные для разных типов башен)
    public Dictionary<string, object> CustomParameters { get; set; }

    public TowerConfig()
    {
        Scale = 1f;
        CustomParameters = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Получить цвет из hex строки
    /// </summary>
    public Color GetTintColor()
    {
        if (string.IsNullOrEmpty(TintColorHex))
            return Color.White;
            
        try
        {
            // Парсим #RRGGBB или #RRGGBBAA
            string hex = TintColorHex.TrimStart('#');
            
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;
            
            return new Color(r, g, b, a);
        }
        catch
        {
            return Color.White;
        }
    }
}