using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.UI;
using System;
using SimulationEngine.BulletRelated.Behaviors;
using SimulationEngine.WaveRelated;
using SimulationEngine.EnemyRelated;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.BulletRelated;
using System.Collections.Generic;

namespace SimulationEngine;

/// <summary>
/// Главный менеджер игры - координирует взаимодействие UI, карты, башен и врагов
/// </summary>
public class GameManager
{
    public UIManager UIManager { get; private set; }
    public GameMap Map { get; private set; }
    internal TowerController TowerController { get; private set; }
    internal WaveController WaveController { get; private set; }
    internal EnemyController EnemyController { get; private set; }
    internal DamageDealerController DamageDealerController { get; private set; }
    
    public Texture2D DefaultTowerTexture { get; set; }
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

    public static GameManager getInstance(int screenWidth, int screenHeight, GameMap map, TowerController towerController, WaveController waveController = null, EnemyController enemyController = null, DamageDealerController damageDealerController = null, Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions = null)
    {
        if (_instance != null)
        {
            return _instance;
        }
        _instance = new GameManager(screenWidth, screenHeight, map, towerController, waveController, enemyController, damageDealerController, towerDefinitions);
        return _instance;
    }

    public static void ResetInstance()
    {
        _instance = null;
    }

    private GameManager(int screenWidth, int screenHeight, GameMap map, TowerController towerController, WaveController waveController = null, EnemyController enemyController = null, DamageDealerController damageDealerController = null, Dictionary<string, LevelLoader.TowerDefinition> towerDefinitions = null)
    {
        UIManager = new UIManager(screenWidth, screenHeight);
        Map = map;
        TowerController = towerController;
        WaveController = waveController;
        EnemyController = enemyController;
        DamageDealerController = damageDealerController ?? DamageDealerController.GetInstance(null);
        
        _inputHandler = new GameInputHandler(UIManager, Map, TowerController);
        
        UIManager.OnStartWaveRequested += StartWave;
        
        // Добавляем доступные башни в UI
        if (towerDefinitions != null && towerDefinitions.Count > 0)
        {
            foreach (var def in towerDefinitions.Values)
            {
                var behavior = new TowerRelated.Behaviors.DefinitionTowerBehavior(def, new StandardBulletBehavior(25f, 500f, 500f));
                UIManager.AddAvailableTower(behavior, def);
            }
        }
        else
        {
            // fallback на базовую башню
            var basicDef = new LevelLoader.TowerDefinition
            {
                Id = "basic_tower",
                Name = "Basic Tower",
                Cost = 100,
                Range = 150f,
                FireRate = 1f,
                UpgradeLevels = new List<LevelLoader.UpgradeLevelData>()
            };
            var behavior = new TowerRelated.Behaviors.DefinitionTowerBehavior(basicDef, new StandardBulletBehavior(25f, 300f, 500f));
            UIManager.AddAvailableTower(behavior, basicDef);
        }
    }

    public void Update(GameTime gameTime)
    {
        UIManager.Update(gameTime);
        _inputHandler.Update();

        TowerController.Update(gameTime);
        EnemyController?.Update(gameTime);
        DamageDealerController?.Update(gameTime);
        WaveController?.Update(gameTime);
        
        // Обновляем Lives на основе здоровья базы
        if (Map.DefensePoints.Count > 0)
        {
            UIManager.Lives = Map.DefensePoints[0].Health;
        }
        
        // Проверка на поражение
        if (UIManager.Lives <= 0)
        {
            Defeat?.Invoke();
        }
        // Чекаем победу
        if (WaveController.CurrentWaveIndex >= WaveController.TotalWaves && !WaveController.IsWaveActive)
        {
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

    private void StartWave()
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
