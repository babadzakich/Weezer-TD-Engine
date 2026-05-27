using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;
using SimulationEngine.Network;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;
using SimulationEngine.UI;
using SimulationEngine.WaveRelated;

namespace SimulationEngine;

/// <summary>
/// Главный менеджер игры - координирует взаимодействие UI, карты, башен и врагов
/// </summary>
public class GameManager
{
    public UIManager UIManager { get; private set; }
    public GameMap Map { get; private set; }
    public TowerController TowerController { get; private set; }
    public WaveController WaveController { get; private set; }
    public EnemyController EnemyController { get; private set; }
    public DamageDealerController DamageDealerController { get; private set; }
    public Dictionary<string, LevelLoader.TowerDefinition> TowerDefinitions { get; private set; }

    public Texture2D DefaultTowerTexture { get; set; }
    public Texture2D DefaultEnemyTexture { get; set; }
    public Texture2D DefaultBulletTexture { get; set; }

    private readonly GameInputHandler _inputHandler;
    private ILobbyDiscovery _discovery;

    public event Action Defeat;
    public event Action Win;

    private static GameManager _instance;

    public static GameManager GetInstance()
    {
        if (_instance == null)
        {
            throw new InvalidOperationException("GameManager is not initialized. Call getInstance with parameters first.");
        }

        return _instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    public static GameManager getInstance(
        int screenWidth,
        int screenHeight,
        GameMap map,
        int startingMoney,
        int startingLives,
        TowerController towerController,
        List<string> towerNames,
        Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions,
        WaveController waveController = null,
        EnemyController enemyController = null,
        DamageDealerController damageDealerController = null)
    {
        if (_instance != null)
        {
            return _instance;
        }

        _instance = new GameManager(
            screenWidth,
            screenHeight,
            map,
            startingMoney,
            startingLives,
            towerController,
            towerNames,
            towerDefinitions,
            waveController,
            enemyController,
            damageDealerController);

        return _instance;
    }

    public static GameManager getInstance(
        int screenWidth,
        int screenHeight,
        GameMap map,
        TowerController towerController,
        WaveController waveController = null,
        EnemyController enemyController = null,
        DamageDealerController damageDealerController = null,
        Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions = null)
    {
        return getInstance(
            screenWidth,
            screenHeight,
            map,
            100,
            20,
            towerController,
            null,
            towerDefinitions,
            waveController,
            enemyController,
            damageDealerController);
    }

    private GameManager(
        int screenWidth,
        int screenHeight,
        GameMap map,
        int startingMoney,
        int startingLives,
        TowerController towerController,
        List<string> towerNames,
        Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions,
        WaveController waveController = null,
        EnemyController enemyController = null,
        DamageDealerController damageDealerController = null)
    {
        UIManager = new UIManager(screenWidth, screenHeight);
        Map = map;
        TowerController = towerController;
        WaveController = waveController;
        EnemyController = enemyController;
        DamageDealerController = damageDealerController ?? DamageDealerController.GetInstance(null);
        TowerDefinitions = towerDefinitions ?? new Dictionary<string, LevelLoader.TowerDefinition>();

        _inputHandler = new GameInputHandler(UIManager, Map, TowerController);

        UIManager.OnStartWaveRequested += RequestStartWave;

        UIManager.Money = startingMoney;
        UIManager.Lives = startingLives;
        if (Map.DefensePoints.Count > 0)
        {
            Map.DefensePoints[0].Health = startingLives;
        }

        InitializeAvailableTowers(towerNames, TowerDefinitions);
    }

    private void InitializeAvailableTowers(
        List<string> towerNames,
        Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions)
    {
        if (towerDefinitions != null && towerDefinitions.Count > 0)
        {
            var upgradeTargets = towerDefinitions.Values
                .Where(def => def.Upgrades != null)
                .SelectMany(def => def.Upgrades)
                .Select(upgrade => upgrade.TargetTowerId)
                .Where(targetId => !string.IsNullOrWhiteSpace(targetId))
                .ToHashSet();

            foreach (var def in towerDefinitions.Values.Where(def => !upgradeTargets.Contains(def.Id)))
            {
                var behavior = TowerBehaviorFactory.CreateTowerBehavior(def);
                UIManager.AddAvailableTower(behavior, def);
            }

            return;
        }

        if (towerNames != null && towerNames.Count > 0)
        {
            foreach (var towerName in towerNames)
            {
                var behavior = TowerBehaviorFactory.CreateFromRegisteredName(towerName);
                UIManager.AddAvailableTower(behavior, behavior.Definition);
            }

            return;
        }

        var basicDef = new LevelLoader.TowerDefinition
        {
            Id = "basic_tower",
            Name = "Basic Tower",
            Cost = 100,
            Range = 150f,
            FireRate = 1f,
            Damage = 25f
        };

        var fallbackBehavior = new DefinitionTowerBehavior(
            basicDef,
            new StandardBulletBehavior(25f, 300f, 500f));
        UIManager.AddAvailableTower(fallbackBehavior, basicDef);
    }

    public void Update(GameTime gameTime)
    {
        UIManager.Update(gameTime);
        _inputHandler.Update();

        TowerController.Update(gameTime);
        WaveController?.Update(gameTime);
        EnemyController?.Update(gameTime);
        DamageDealerController?.Update(gameTime);

        if (Map.DefensePoints.Count > 0)
        {
            UIManager.Lives = Map.DefensePoints[0].Health;
        }

        if (UIManager.Lives <= 0)
        {
            Console.WriteLine("Defeat detected");
            Defeat?.Invoke();
        }

        if (WaveController != null &&
            WaveController.CurrentWaveIndex >= WaveController.TotalWaves &&
            !WaveController.IsWaveActive &&
            (EnemyController == null || EnemyController.Enemies.Count == 0))
        {
            Console.WriteLine("Win detected");
            Win?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font = null)
    {
        Map.Draw(spriteBatch, pixelTexture);
        TowerController.Draw(spriteBatch);
        EnemyController?.Draw(spriteBatch);
        DamageDealerController?.Draw(spriteBatch);
        UIManager.Draw(spriteBatch, pixelTexture, font);
    }

    public void StartWave() => RequestStartWave();

    private void RequestStartWave()
    {
        if (_discovery != null)
            _discovery.BroadcastWaveStart();
        else
            ApplyStartWave();
    }

    public void ApplyStartWave()
    {
        if (WaveController != null &&
            !WaveController.IsWaveActive &&
            WaveController.CurrentWaveIndex < WaveController.TotalWaves)
        {
            WaveController.StartNextWave();
        }
    }

    public void AttachDiscovery(ILobbyDiscovery discovery)
    {
        if (_discovery != null) return;
        _discovery = discovery;
        if (_discovery == null) return;
        _discovery.OnRemoteTowerPlace += (bzId, tdId) => _inputHandler.ApplyTowerPlacement(bzId, tdId);
        _discovery.OnRemoteWaveStart += ApplyStartWave;
        _inputHandler.Discovery = _discovery;
        _inputHandler.TowerDefinitions = TowerDefinitions;
    }

    public void AddMoney(int amount)
    {
        UIManager.Money += amount;
    }

    public void RemoveLife()
    {
        UIManager.Lives--;
    }
}
