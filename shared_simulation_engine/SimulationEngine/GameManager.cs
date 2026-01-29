using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.BulletRelated;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.EnemyRelated;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.TowerRelated.Behaviors;
using SimulationEngine.UI;
using SimulationEngine.WaveRelated;
using static SimulationEngine.LevelLoader;

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
    
    public Texture2D DefaultTowerTexture { get; set; }
    public Texture2D DefaultEnemyTexture { get; set; }
    public Texture2D DefaultBulletTexture { get; set; }

    private GameInputHandler _inputHandler;

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

    public static GameManager getInstance(int screenWidth, int screenHeight, GameMap map, int startingMoney, int startingLives, TowerController towerController, List<string> towerNames, Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions, WaveController waveController = null, EnemyController enemyController = null, DamageDealerController damageDealerController = null)
    {
        if (_instance != null)
        {
            return _instance;
        }
        _instance = new GameManager(screenWidth, screenHeight, map, startingMoney, startingLives, towerController, towerNames, towerDefinitions, waveController, enemyController, damageDealerController);
        return _instance;
    }

    private GameManager(int screenWidth, int screenHeight, GameMap map, int startingMoney, int startingLives, TowerController towerController, List<string> towerNames, Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions, WaveController waveController = null, EnemyController enemyController = null, DamageDealerController damageDealerController = null)
    {
        UIManager = new UIManager(screenWidth, screenHeight);
        Map = map;
        TowerController = towerController;
        WaveController = waveController;
        EnemyController = enemyController;
        DamageDealerController = damageDealerController ?? DamageDealerController.GetInstance(null);
        
        _inputHandler = new GameInputHandler(UIManager, Map, TowerController);
        
        UIManager.OnStartWaveRequested += StartWave;

        // Existing design assume that health is the health of the first defense point
        // So I decide to stick to that
        UIManager.Money = startingMoney;
        Map.DefensePoints[0].Health = startingLives;

        // Добавляем доступные башни в UI
        foreach (var towerName in towerNames)
        {
            UIManager.AddAvailableTower(TowerRelated.TowerBehaviorRegistry.create(towerName));
        }

        // Добавляем стандартную базовую башню
        //UIManager.AddAvailableTower(new TowerRelated.Behaviors.BasicTowerBehavior("basic_tower", "Basic Tower", new StandardBulletBehavior(25f, 300f, 500f), 100, 150f, 1f));
    }

    public void Update(GameTime gameTime)
    {
        UIManager.Update(gameTime);
        _inputHandler.Update();

        TowerController.Update(gameTime);
        WaveController?.Update(gameTime);
        EnemyController?.Update(gameTime);
        DamageDealerController?.Update(gameTime);
        
        // Обновляем Lives на основе здоровья базы
        if (Map.DefensePoints.Count > 0)
        {
            UIManager.Lives = Map.DefensePoints[0].Health;
        }
        
        // Проверка на поражение
        if (UIManager.Lives <= 0)
        {
            Console.WriteLine("Defeat detected");
            Defeat?.Invoke();
        }
        // Чекаем победу
        if (WaveController != null && WaveController.CurrentWaveIndex >= WaveController.TotalWaves && !WaveController.IsWaveActive && EnemyController.Enemies.Count == 0)
        {
            Console.WriteLine("Win detected");
            Win?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font = null)
    {
        Map.Draw(spriteBatch, pixelTexture);
        TowerController.Draw(spriteBatch);
        if (EnemyController != null)
            EnemyController.Draw(spriteBatch);
        DamageDealerController?.Draw(spriteBatch);
        UIManager.Draw(spriteBatch, pixelTexture, font);
    }

    public void StartWave()
    {
        if (WaveController != null && !WaveController.IsWaveActive && WaveController.CurrentWaveIndex < WaveController.TotalWaves)
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
