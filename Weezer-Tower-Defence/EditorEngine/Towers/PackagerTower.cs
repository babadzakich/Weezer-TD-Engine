using System.IO;
using System.IO.Compression;
using System.Text.Json;
using SimulationEngine.TowerRelated;

namespace EditorEngine.Towers
{
    public static class TowerPackageSaver
    {
        public static void Save(string outputPath, TowerConfig config)
        {
            // Гарантируем папку
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // Перезаписываем архив
            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using var zip = ZipFile.Open(outputPath, ZipArchiveMode.Create);

            var entry = zip.CreateEntry("tower.json");

            using var stream = entry.Open();
            using var writer = new StreamWriter(stream);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            writer.Write(JsonSerializer.Serialize(config, options));
        }
    }
}
