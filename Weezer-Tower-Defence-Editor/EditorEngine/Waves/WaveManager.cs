using System;
using System.Collections.Generic;
using System.Linq;

namespace EditorEngine.Waves;

/// <summary>
/// Менеджер для управления волнами врагов
/// </summary>
public class WaveManager
{
    private readonly WaveSet _waveSet;

    public WaveManager(string mapId)
    {
        _waveSet = new WaveSet { MapId = mapId };
    }

    public WaveManager(WaveSet existingWaveSet)
    {
        _waveSet = existingWaveSet ?? throw new ArgumentNullException(nameof(existingWaveSet));
    }

    /// <summary>
    /// Получить все волны
    /// </summary>
    public IReadOnlyList<Wave> GetWaves() => _waveSet.Waves.AsReadOnly();

    /// <summary>
    /// Получить количество волн
    /// </summary>
    public int WaveCount => _waveSet.Waves.Count;

    /// <summary>
    /// Получить волну по индексу
    /// </summary>
    public Wave GetWave(int index)
    {
        if (index < 0 || index >= _waveSet.Waves.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return _waveSet.Waves[index];
    }

    /// <summary>
    /// Добавить новую пустую волну в конец
    /// </summary>
    /// <returns>Индекс добавленной волны</returns>
    public int AddWave()
    {
        var wave = new Wave 
        { 
            Index = _waveSet.Waves.Count 
        };
        
        _waveSet.Waves.Add(wave);
        return wave.Index;
    }

    /// <summary>
    /// Добавить волну с конфигурацией
    /// </summary>
    /// <param name="spawnPointId">ID точки спавна</param>
    /// <param name="enemyTypeId">ID типа врага</param>
    /// <param name="count">Количество врагов</param>
    /// <returns>Индекс добавленной волны</returns>
    public int AddWave(string spawnPointId, string enemyTypeId, int count)
    {
        if (string.IsNullOrEmpty(spawnPointId))
            throw new ArgumentException("SpawnPointId cannot be empty", nameof(spawnPointId));
        
        if (string.IsNullOrEmpty(enemyTypeId))
            throw new ArgumentException("EnemyTypeId cannot be empty", nameof(enemyTypeId));
        
        if (count <= 0)
            throw new ArgumentException("Count must be positive", nameof(count));

        var wave = new Wave 
        { 
            Index = _waveSet.Waves.Count 
        };

        wave.Spawns.Add(new EnemySpawn
        {
            SpawnPointId = spawnPointId,
            EnemyTypeId = enemyTypeId,
            Count = count
        });
        
        _waveSet.Waves.Add(wave);
        return wave.Index;
    }

    /// <summary>
    /// Удалить волну по индексу
    /// </summary>
    public bool RemoveWave(int index)
    {
        if (index < 0 || index >= _waveSet.Waves.Count)
            return false;

        _waveSet.Waves.RemoveAt(index);
        
        // Обновляем индексы оставшихся волн
        for (int i = index; i < _waveSet.Waves.Count; i++)
        {
            _waveSet.Waves[i].Index = i;
        }
        
        return true;
    }

    /// <summary>
    /// Добавить спавн врагов в существующую волну
    /// </summary>
    public bool AddEnemySpawnToWave(int waveIndex, string spawnPointId, string enemyTypeId, int count)
    {
        if (waveIndex < 0 || waveIndex >= _waveSet.Waves.Count)
            return false;
        
        if (string.IsNullOrEmpty(spawnPointId) || string.IsNullOrEmpty(enemyTypeId) || count <= 0)
            return false;

        var wave = _waveSet.Waves[waveIndex];
        wave.Spawns.Add(new EnemySpawn
        {
            SpawnPointId = spawnPointId,
            EnemyTypeId = enemyTypeId,
            Count = count
        });
        
        return true;
    }

    /// <summary>
    /// Удалить спавн врагов из волны
    /// </summary>
    public bool RemoveEnemySpawnFromWave(int waveIndex, int spawnIndex)
    {
        if (waveIndex < 0 || waveIndex >= _waveSet.Waves.Count)
            return false;

        var wave = _waveSet.Waves[waveIndex];
        
        if (spawnIndex < 0 || spawnIndex >= wave.Spawns.Count)
            return false;

        wave.Spawns.RemoveAt(spawnIndex);
        return true;
    }

    /// <summary>
    /// Очистить все волны
    /// </summary>
    public void ClearAllWaves()
    {
        _waveSet.Waves.Clear();
    }

    /// <summary>
    /// Получить WaveSet для сохранения
    /// </summary>
    public WaveSet GetWaveSet() => _waveSet;

    /// <summary>
    /// Получить общее количество врагов в волне
    /// </summary>
    public int GetTotalEnemyCountInWave(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= _waveSet.Waves.Count)
            return 0;

        return _waveSet.Waves[waveIndex].Spawns.Sum(s => s.Count);
    }

    /// <summary>
    /// Проверить, пустая ли волна
    /// </summary>
    public bool IsWaveEmpty(int waveIndex)
    {
        if (waveIndex < 0 || waveIndex >= _waveSet.Waves.Count)
            return true;

        return _waveSet.Waves[waveIndex].Spawns.Count == 0;
    }
}
