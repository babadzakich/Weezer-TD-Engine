using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.MapRelated;
using SimulationEngine.TowerRelated;
using SimulationEngine.UI;
using System;
using SimulationEngine.BulletRelated.Behaviors;

namespace SimulationEngine;

/// <summary>
/// Главный менеджер игры - координирует взаимодействие UI, карты, башен и врагов
/// </summary>
public class GameManager
{
    public UIManager UIManager { get; private set; }
    public GameMap Map { get; private set; }
    internal TowerController TowerController { get; private set; }
    
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

    public static GameManager getInstance(int screenWidth, int screenHeight, GameMap map, TowerController towerController)
    {
        if (_instance != null)
        {
            return _instance;
        }
        _instance = new GameManager(screenWidth, screenHeight, map, towerController);
        return _instance;
    }

    private GameManager(int screenWidth, int screenHeight, GameMap map, TowerController towerController)
    {
        UIManager = new UIManager(screenWidth, screenHeight);
        Map = map;
        TowerController = towerController;
        
        _inputHandler = new GameInputHandler(UIManager, Map, TowerController);
        
        UIManager.OnStartWaveRequested += StartWave;
    }

    public void Update(GameTime gameTime)
    {
        UIManager.Update(gameTime);
        _inputHandler.Update();
        
        // Проверка на поражение
        if (UIManager.Lives <= 0)
        {
            OnGameOver?.Invoke();
        }
    }

    private void StartWave()
    {
        // TODO: Запуск волны врагов
        UIManager.Wave++;
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
