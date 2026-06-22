using System;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SimulationEngine.TowerRelated;

public class TowerController : Controller
{
    public readonly List<Tower> towers;
    private static TowerController _instance;
    private readonly Dictionary<string, Microsoft.Xna.Framework.Graphics.Texture2D> _textureCache = new();
    private readonly Dictionary<int, Tower> _towersById = new();
    private int _nextNetworkId = 1;

    private readonly Game _engine;
    public Microsoft.Xna.Framework.Graphics.Texture2D DefaultTexture { get; set; }
    
    private TowerController(Game engine)
    {
        towers = new List<Tower>();
        _engine = engine;
    }

    public static TowerController GetInstance(Game engine)
    {
        if (_instance == null)
        {
            _instance = new TowerController(engine);
        }
        return _instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    public void AddTower(Tower tower)
    {
        if (tower.NetworkId < 0)
            tower.NetworkId = _nextNetworkId++;
        _towersById[tower.NetworkId] = tower;
        if (tower.Texture == null)
            tower.Texture = GetTowerTexture(tower.Definition);
        towers.Add(tower);
    }

    public Tower GetByNetworkId(int networkId)
        => _towersById.TryGetValue(networkId, out var t) ? t : null;

    public void RemoveTower(Tower tower)
    {
        if (tower.NetworkId >= 0)
            _towersById.Remove(tower.NetworkId);
        towers.Remove(tower);
    }

    public Microsoft.Xna.Framework.Graphics.Texture2D GetTowerTexture(LevelLoader.TowerDefinition definition)
    {
        if (definition == null) return DefaultTexture;

        // Пытаемся найти в кэше
        if (_textureCache.TryGetValue(definition.Id, out var cached)) return cached;

        // Пытаемся загрузить из папки кастомных башен
        string customPath = System.IO.Path.Combine(Infrastructure.PathService.GetEntityDllDirectory("towers"), $"{definition.Id}.png");
        
        if (System.IO.File.Exists(customPath))
        {
            try
            {
                using (var stream = System.IO.File.OpenRead(customPath))
                {
                    var texture = Microsoft.Xna.Framework.Graphics.Texture2D.FromStream(_engine.GraphicsDevice, stream);
                    _textureCache[definition.Id] = texture;
                    Console.WriteLine($"Loaded custom tower sprite: {customPath}");
                    return texture;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading custom tower sprite {definition.Id}: {ex.Message}");
            }
        }

        return DefaultTexture;
    }

    public void Update(GameTime deltaTime)
    {
        foreach (var tower in towers)
        {
            tower.Update(deltaTime);
        }
    }

    public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
    {
        foreach (var tower in towers)
        {
            tower.Draw(spriteBatch);
        }
    }
}