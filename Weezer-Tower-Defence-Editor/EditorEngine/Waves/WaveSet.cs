using System.Collections.Generic;

namespace EditorEngine.Waves;

public class WaveSet
{
    public string MapId { get; set; }
    public List<Wave> Waves { get; } = new();
}
