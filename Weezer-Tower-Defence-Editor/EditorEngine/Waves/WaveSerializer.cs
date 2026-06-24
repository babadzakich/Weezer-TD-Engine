using System.Text.Json;
using System.IO;

namespace EditorEngine.Waves;

public static class WaveSerializer
{
    public static void Save(WaveSet waveSet, string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(waveSet, options);
        File.WriteAllText(path, json);
    }

    public static WaveSet Load(string path)
    {
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<WaveSet>(json);
    }
}
