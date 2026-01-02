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
    
    private GameInputHandler _inputHandler;

    public event Action OnGameOver;

    private static GameManager _instance;

    public static GameManager GetInstance()
    {
        if (_instance == null)
        {
            throw new InvalidOperationException("GameManager is not initialized. Call getInstance with parameters first.");
        }
        return _instance;
    }

    public static GameManager getInstance(int screenWidth, int screenHeight, GameMap map, TowerController towerController, WaveController waveController = null, EnemyController enemyController = null, DamageDealerController damageDealerController = null)
    {
        if (_instance != null)
        {
            return _instance;
        }
        _instance = new GameManager(screenWidth, screenHeight, map, towerController, waveController, enemyController, damageDealerController);
        return _instance;
    }

    private GameManager(int screenWidth, int screenHeight, GameMap map, TowerController towerController, WaveController waveController = null, EnemyController enemyController = null, DamageDealerController damageDealerController = null)
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
        UIManager.AddAvailableTower(new TowerRelated.Behaviors.BasicTowerBehavior("basic_tower", "Basic Tower", new StandardBulletBehavior(25f, 300f, 500f), 100, 150f, 1f));
    }

    public void Update(GameTime gameTime)
    {
        UIManager.Update(gameTime);
        _inputHandler.Update();

        TowerController.Update(gameTime);
        EnemyController?.Update(gameTime);
        DamageDealerController?.Update(gameTime);
        
        // Обновляем Lives на основе здоровья базы
        if (Map.DefensePoints.Count > 0)
        {
            UIManager.Lives = Map.DefensePoints[0].Health;
        }
        
        // Проверка на поражение или конец волн
        if (UIManager.Lives <= 0 || (WaveController.CurrentWaveIndex >= WaveController.TotalWaves && !WaveController.IsWaveActive))
        {
            OnGameOver?.Invoke();
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
