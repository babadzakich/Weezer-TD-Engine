using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace SimulationEngine.MapRelated;

/// <summary>
/// Путь для движения врагов - последовательность точек со сплайн-интерполяцией
/// </summary>
public class Path
{
    public string Id { get; set; }
    public List<Vector2> Waypoints { get; private set; }
    
    /// <summary>
    /// К какой точке защиты ведёт этот путь
    /// </summary>
    public string DefensePointId { get; set; }
    
    /// <summary>
    /// Использовать ли сплайн для интерполяции пути
    /// </summary>
    public bool UseSmoothPath { get; set; }
    
    /// <summary>
    /// Точность сплайна (количество промежуточных точек между waypoints)
    /// </summary>
    public int SplineResolution { get; set; }
    
    private List<Vector2> _smoothPath;
    private bool _isDirty;

    public Path(string id, string defensePointId, bool useSmoothPath = true, int splineResolution = 20)
    {
        Id = id;
        DefensePointId = defensePointId;
        UseSmoothPath = useSmoothPath;
        SplineResolution = splineResolution;
        Waypoints = new List<Vector2>();
        _smoothPath = new List<Vector2>();
        _isDirty = true;
    }

    public void AddWaypoint(Vector2 point)
    {
        Waypoints.Add(point);
        _isDirty = true;
    }

    public void AddWaypoints(params Vector2[] points)
    {
        Waypoints.AddRange(points);
        _isDirty = true;
    }

    /// <summary>
    /// Получить сглаженный путь (Catmull-Rom сплайн)
    /// </summary>
    public List<Vector2> GetSmoothPath()
    {
        if (!UseSmoothPath)
            return Waypoints;
            
        if (_isDirty)
        {
            GenerateSmoothPath();
            _isDirty = false;
        }
        
        return _smoothPath;
    }

    private void GenerateSmoothPath()
    {
        _smoothPath.Clear();
        
        if (Waypoints.Count < 2)
        {
            _smoothPath.AddRange(Waypoints);
            return;
        }

        // Catmull-Rom сплайн
        for (int i = 0; i < Waypoints.Count - 1; i++)
        {
            Vector2 p0 = i > 0 ? Waypoints[i - 1] : Waypoints[i];
            Vector2 p1 = Waypoints[i];
            Vector2 p2 = Waypoints[i + 1];
            Vector2 p3 = i < Waypoints.Count - 2 ? Waypoints[i + 2] : Waypoints[i + 1];

            for (int j = 0; j < SplineResolution; j++)
            {
                float t = j / (float)SplineResolution;
                Vector2 point = Vector2.CatmullRom(p0, p1, p2, p3, t);
                _smoothPath.Add(point);
            }
        }
        
        // Добавляем последнюю точку
        _smoothPath.Add(Waypoints[Waypoints.Count - 1]);
    }

    /// <summary>
    /// Получить позицию на пути по проценту (0.0 - 1.0)
    /// </summary>
    public Vector2 GetPositionAtProgress(float progress)
    {
        progress = MathHelper.Clamp(progress, 0f, 1f);
        
        var path = UseSmoothPath ? GetSmoothPath() : Waypoints;
        if (path.Count == 0) return Vector2.Zero;
        if (path.Count == 1) return path[0];

        float exactIndex = progress * (path.Count - 1);
        int index = (int)exactIndex;
        
        if (index >= path.Count - 1)
            return path[path.Count - 1];

        float t = exactIndex - index;
        return Vector2.Lerp(path[index], path[index + 1], t);
    }

    /// <summary>
    /// Получить следующую точку пути
    /// </summary>
    public Vector2? GetNextWaypoint(int currentIndex)
    {
        if (currentIndex < 0 || currentIndex >= Waypoints.Count - 1)
            return null;
        
        return Waypoints[currentIndex + 1];
    }

    /// <summary>
    /// Получить финальную точку пути
    /// </summary>
    public Vector2? GetFinalWaypoint()
    {
        return Waypoints.Count > 0 ? Waypoints[Waypoints.Count - 1] : null;
    }
    
    /// <summary>
    /// Получить общую длину пути
    /// </summary>
    public float GetPathLength()
    {
        var path = UseSmoothPath ? GetSmoothPath() : Waypoints;
        float length = 0f;
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            length += Vector2.Distance(path[i], path[i + 1]);
        }
        
        return length;
    }
}
