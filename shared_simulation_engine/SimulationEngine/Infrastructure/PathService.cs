using System;
using System.IO;

namespace SimulationEngine.Infrastructure
{
    /// <summary>
    /// Сервис для получения путей к ресурсам, сохранениям и т.д.
    /// </summary>
    public static class PathService
    {
        private static readonly string localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WeezerTowerDefence");
        private static readonly string developmentResourcesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Content");
        public static string AppDataDirectory => localAppData;
        public static string EditorDirectory => Path.Combine(localAppData, "Editor", "Custom");
        public static string SavesDirectory => Path.Combine(localAppData, "Saves");
        public static string DLLsDirectory => Path.Combine(localAppData, "DLLs");
        public static string LevelsDirectory => Path.Combine(localAppData, "Levels");
        public static string CommonDirectory => Path.Combine(localAppData, "common");

        static PathService()
        {
            if (!Directory.Exists(localAppData))
            {
                _initializeDirectories();
                // throw new DirectoryNotFoundException($"Development resources directory not found: {developmentResourcesDirectory}");
            }
        }
        
        private static void _initializeDirectories()
        {
            Directory.CreateDirectory(localAppData);
            Directory.CreateDirectory(EditorDirectory);
            Directory.CreateDirectory(SavesDirectory);
            Directory.CreateDirectory(DLLsDirectory);
            Directory.CreateDirectory(LevelsDirectory);
            Directory.CreateDirectory(CommonDirectory);
        }

        public static string GetLevelArchivePath(string levelId) =>
            Path.Combine(LevelsDirectory, levelId.EndsWith(".zip") ? levelId : $"{levelId}.zip");

        public static string GetCommonFilePath(string fileName) =>
            Path.Combine(CommonDirectory, fileName);

    }
    
}