using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SimulationEngine.Persistence;

/// <summary>
/// Менеджер для сохранения и загрузки игры
/// </summary>
public class SaveManager
{
    private readonly string _saveDirectory;
    private const string SAVE_EXTENSION = ".wzsave";

    public SaveManager(string saveDirectory = "Saves")
    {
        _saveDirectory = saveDirectory;
        
        // Создаём папку для сохранений если её нет
        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
        }
    }

    /// <summary>
    /// Сохранить игру
    /// </summary>
    public bool SaveGame(SaveData saveData, string saveName)
    {
        try
        {
            saveData.SaveName = saveName;
            saveData.SaveTime = DateTime.Now;
            
            string filePath = GetSaveFilePath(saveName);
            string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving game: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Загрузить игру
    /// </summary>
    public SaveData LoadGame(string saveName)
    {
        try
        {
            string filePath = GetSaveFilePath(saveName);
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Save file not found: {filePath}");
                return null;
            }
            
            string json = File.ReadAllText(filePath);
            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
            
            return saveData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading game: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Получить список всех сохранений
    /// </summary>
    public List<SaveInfo> GetAllSaves()
    {
        List<SaveInfo> saves = new List<SaveInfo>();
        
        try
        {
            string[] files = Directory.GetFiles(_saveDirectory, "*" + SAVE_EXTENSION);
            
            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    SaveData saveData = JsonSerializer.Deserialize<SaveData>(json);
                    
                    saves.Add(new SaveInfo
                    {
                        SaveName = saveData.SaveName,
                        SaveTime = saveData.SaveTime,
                        FilePath = file,
                        Wave = saveData.GameState?.CurrentWave ?? 0,
                        Money = saveData.GameState?.Money ?? 0,
                        Lives = saveData.GameState?.Lives ?? 0
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading save file {file}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting saves: {ex.Message}");
        }
        
        // Сортируем по времени сохранения (новые сверху)
        saves.Sort((a, b) => b.SaveTime.CompareTo(a.SaveTime));
        
        return saves;
    }

    /// <summary>
    /// Удалить сохранение
    /// </summary>
    public bool DeleteSave(string saveName)
    {
        try
        {
            string filePath = GetSaveFilePath(saveName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting save: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Проверить, существует ли сохранение
    /// </summary>
    public bool SaveExists(string saveName)
    {
        string filePath = GetSaveFilePath(saveName);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Автосохранение
    /// </summary>
    public bool AutoSave(SaveData saveData)
    {
        return SaveGame(saveData, "autosave");
    }

    /// <summary>
    /// Загрузить автосохранение
    /// </summary>
    public SaveData LoadAutoSave()
    {
        return LoadGame("autosave");
    }

    private string GetSaveFilePath(string saveName)
    {
        // Убираем недопустимые символы из имени файла
        string sanitizedName = SanitizeFileName(saveName);
        return Path.Combine(_saveDirectory, sanitizedName + SAVE_EXTENSION);
    }

    private string SanitizeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
}

/// <summary>
/// Информация о сохранении для отображения в списке
/// </summary>
public class SaveInfo
{
    public string SaveName { get; set; }
    public DateTime SaveTime { get; set; }
    public string FilePath { get; set; }
    public int Wave { get; set; }
    public int Money { get; set; }
    public int Lives { get; set; }

    public string GetDisplayText()
    {
        return $"{SaveName} - Wave {Wave} - {SaveTime:yyyy-MM-dd HH:mm}";
    }
}
