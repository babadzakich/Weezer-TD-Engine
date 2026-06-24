using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;
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

    public event Action Defeat;
    public event Action Win;
    /// <summary>Fired when the client loses network connectivity (10-s countdown expired).</summary>
    public event Action Disconnected;

    public bool IsNetworkClient { get; private set; } = false;
    public Network.GameSyncManager SyncManager { get; private set; }

    private bool _isDisconnecting;
    private double _disconnectTimer;

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

        UIManager.OnStartWaveRequested += StartWave;

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

    /// <summary>Switches this instance into network-client mode (no local simulation).</summary>
    public void SetNetworkMode(bool isClient, Network.GameSyncManager syncManager)
    {
        IsNetworkClient = isClient;
        SyncManager = syncManager;
        _inputHandler.SyncManager = syncManager;
        _inputHandler.IsNetworkClient = isClient;
    }

    public void TriggerDefeat() => Defeat?.Invoke();
    public void TriggerWin()    => Win?.Invoke();

    /// <summary>
    /// Called by GameSyncManager when this client wins a Raft election.
    /// Switches from client-only mode (applying deltas) to host mode (running simulation).
    /// </summary>
    public void PromoteToHost()
    {
        IsNetworkClient = false;
        _inputHandler.IsNetworkClient = false;
        UIManager.StartWaveButton.IsEnabled = true;
        Console.WriteLine("[GameManager] Promoted to game master.");
    }

    /// <summary>
    /// Called when the network is detected as broken (Raft couldn't reach any peer).
    /// Shows a connection-lost overlay and fires Disconnected after 10 seconds.
    /// </summary>
    public void HandleNetworkLost()
    {
        if (_isDisconnecting) return;
        _isDisconnecting = true;
        _disconnectTimer = 10.0;
        UIManager.ShowConnectionLost(10);
        Console.WriteLine("[GameManager] Network lost — starting 10-s disconnect countdown.");
    }

    public void Update(GameTime gameTime)
    {
        // Connection-lost countdown: block game updates and count down to Disconnected
        if (_isDisconnecting)
        {
            _disconnectTimer -= gameTime.ElapsedGameTime.TotalSeconds;
            int secondsLeft = Math.Max(0, (int)Math.Ceiling(_disconnectTimer));
            UIManager.ShowConnectionLost(secondsLeft);

            if (_disconnectTimer <= 0)
            {
                _isDisconnecting = false;
                Disconnected?.Invoke();
            }
            return;
        }

        if (IsNetworkClient)
        {
            // Apply incoming network state BEFORE processing input so that zone occupancy
            // and tower ownership are current when the UI and input handler run.
            SyncManager?.ApplyIncomingDeltas();
            // Move visual bullets locally (no collision detection — host is authoritative).
            DamageDealerController?.UpdatePositionsOnly(gameTime);
        }

        UIManager.Update(gameTime);
        _inputHandler.Update();

        if (!IsNetworkClient)
        {
            // Host / singleplayer: run full simulation
            TowerController.Update(gameTime);
            WaveController?.Update(gameTime);
            EnemyController?.Update(gameTime);
            DamageDealerController?.Update(gameTime);

            if (Map.DefensePoints.Count > 0)
                UIManager.Lives = Map.DefensePoints[0].Health;

            // Broadcast state to clients if we are the multiplayer host
            SyncManager?.BroadcastTick(gameTime);
        }

        if (UIManager.Lives <= 0)
        {
            Console.WriteLine("Defeat detected");
            Defeat?.Invoke();
        }

        if (!IsNetworkClient &&
            WaveController != null &&
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

    public void StartWave()
    {
        if (WaveController != null &&
            !WaveController.IsWaveActive &&
            WaveController.CurrentWaveIndex < WaveController.TotalWaves)
        {
            WaveController.StartNextWave();
        }
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
