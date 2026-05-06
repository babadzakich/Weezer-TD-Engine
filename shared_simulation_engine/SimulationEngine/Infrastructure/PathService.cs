using System;
using System.IO;

namespace SimulationEngine.Infrastructure
{
    /// <summary>
    /// Сервис для получения путей к ресурсам, сохранениям и т.д.
    /// </summary>
    public static class PathService
    {
        private static readonly string localAppData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence");

        public static string AppDataDirectory => localAppData;
        public static string EditorDirectory => Path.Combine(localAppData, "Editor");
        public static string EditorAssetsDirectory => Path.Combine(EditorDirectory, "Assets");
        public static string EditorMapsDirectory => Path.Combine(EditorDirectory, "Maps");
        public static string SavesDirectory => Path.Combine(localAppData, "Saves");
        public static string DLLsDirectory => Path.Combine(localAppData, "DLLs");
        public static string LevelsDirectory => Path.Combine(localAppData, "Levels");
        public static string CommonDirectory => Path.Combine(localAppData, "common");

        static PathService()
        {
            EnsureInitialized();
        }

        public static void EnsureInitialized()
        {
            Directory.CreateDirectory(localAppData);
            Directory.CreateDirectory(EditorDirectory);
            Directory.CreateDirectory(SavesDirectory);
            Directory.CreateDirectory(DLLsDirectory);
            Directory.CreateDirectory(LevelsDirectory);
            Directory.CreateDirectory(CommonDirectory);
            Directory.CreateDirectory(EditorAssetsDirectory);
            Directory.CreateDirectory(EditorMapsDirectory);

            foreach (var entityType in new[] { "towers", "enemies", "damageDealers" })
            {
                Directory.CreateDirectory(GetEditorEntityDirectory(entityType));
                Directory.CreateDirectory(GetEditorBehaviorDirectory(entityType));
                Directory.CreateDirectory(GetEditorConfigDirectory(entityType));
                Directory.CreateDirectory(GetEntityDllDirectory(entityType));
            }
        }

        public static string GetLevelArchivePath(string levelId) =>
            Path.Combine(LevelsDirectory, levelId.EndsWith(".zip") ? levelId : $"{levelId}.zip");

        public static string GetCommonFilePath(string fileName) =>
            Path.Combine(CommonDirectory, fileName);

        public static string GetEditorEntityDirectory(string entityType) =>
            Path.Combine(EditorDirectory, entityType);

        public static string GetEditorBehaviorDirectory(string entityType) =>
            Path.Combine(GetEditorEntityDirectory(entityType), "behaviors");

        public static string GetEditorConfigDirectory(string entityType) =>
            Path.Combine(GetEditorEntityDirectory(entityType), "configs");

        public static string GetEntityDllDirectory(string entityType) =>
            Path.Combine(DLLsDirectory, entityType);
        public static string GetEditorAssetPath(string assetName) =>
            Path.Combine(EditorAssetsDirectory, assetName);
    }
    
}
