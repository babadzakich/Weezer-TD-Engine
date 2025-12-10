using Microsoft.Xna.Framework;

namespace SimulationEngine.BulletRelated;


public class DamageDealerConfig
{
    public int Damage { get; set; }
        public float Speed { get; set; }
        public string TexturePath { get; set; }
        public Color TintColor { get; set; } = Color.White;
        public float Scale { get; set; } = 1f;
}
