using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SimulationEngine.EnemyRelated;
using SimulationEngine.EnemyRelated.EnemyTypes;
using SimulationEngine.MapRelated;

namespace SimulationEngine.WaveRelated
{
    /// <summary>
    /// Контроллер волн врагов - управление спавном и прогрессом волн
    /// </summary>
    public class WaveController : Controller
    {
        private List<Wave> _waves;
        private int _currentWaveIndex = 0;
        private bool _waveActive = false;
        private float _spawnTimer = 20f;
        private float _spawnInterval = 5f; // Интервал между спавнами врагов (в секундах)
        private Dictionary<System.Type, int> _remainingEnemies; // Оставшиеся враги для спавна в текущей волне
        private Dictionary<System.Type, SpawnPoint> _enemySpawnPoints;
        private EnemyController _enemyController;
        private GameMap _gameMap;
        private Texture2D _enemyTexture;
        private static WaveController _instance;

        private WaveController(EnemyController enemyController, GameMap gameMap)
        {
            _waves = new List<Wave>();
            _enemyController = enemyController;
            _gameMap = gameMap;
            _remainingEnemies = new Dictionary<System.Type, int>();
            _enemySpawnPoints = new Dictionary<System.Type, SpawnPoint>();
        }

        public static WaveController GetInstance(EnemyController enemyController, GameMap gameMap)
        {
            if (_instance == null)
            {
                _instance = new WaveController(enemyController, gameMap);
            }
            return _instance;
        }

        public void SetEnemyTexture(Texture2D texture)
        {
            _enemyTexture = texture;
        }

        public void AddWave(Wave wave)
        {
            _waves.Add(wave);
        }

        public void StartNextWave()
        {
            Console.WriteLine($"StartNextWave called: waveActive={_waveActive}, currentIndex={_currentWaveIndex}, totalWaves={_waves.Count}");
            
            if (_waveActive || _currentWaveIndex >= _waves.Count)
            {
                Console.WriteLine("Cannot start wave - already active or no more waves");
                return;
            }

            _waveActive = true;
            _spawnTimer = 0f;
            
            Wave currentWave = _waves[_currentWaveIndex];
            _remainingEnemies.Clear();
            _enemySpawnPoints.Clear();
            
            Console.WriteLine($"Starting wave {_currentWaveIndex}: {currentWave.Enemies.Count} enemy types");
            
            foreach (var enemyEntry in currentWave.Enemies)
            {
                _remainingEnemies[enemyEntry.Key] = enemyEntry.Value.count;
                _enemySpawnPoints[enemyEntry.Key] = enemyEntry.Value.spawnPoint;
                Console.WriteLine($"  - {enemyEntry.Key.Name}: {enemyEntry.Value.count} enemies at spawn {enemyEntry.Value.spawnPoint.Id}");
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Можно добавить UI для отображения текущей волны
        }

        public void Update(GameTime gameTime)
        {
            if (!_waveActive)
                return;

            _spawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_spawnTimer >= _spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnNextEnemy();
            }

            // Проверяем, закончилась ли волна
            bool allSpawned = true;
            foreach (var count in _remainingEnemies.Values)
            {
                if (count > 0)
                {
                    allSpawned = false;
                    break;
                }
            }


            // ЭТО ПРОСТО БРЕД ЖЕ Е МОЕ
            // Когда все челики добавлены, то волна считается неактивной
            // Сразу после этого игра заканчивается, потому что нет проверки на живых врагов
            if (allSpawned)
            {
                _waveActive = false;
                _currentWaveIndex++;
            }
        }

        private void SpawnNextEnemy()
        {
            Console.WriteLine($"SpawnNextEnemy called: {_remainingEnemies.Count} enemy types remaining");
            
            foreach (var enemyType in _remainingEnemies.Keys)
            {
                if (_remainingEnemies[enemyType] > 0)
                {
                    SpawnPoint spawnPoint = _enemySpawnPoints[enemyType];
                    Console.WriteLine($"Trying to spawn {enemyType.Name} at spawn point {spawnPoint.Id}, pathId={spawnPoint.PathId}");
                    
                    var path = _gameMap.GetPathById(spawnPoint.PathId);
                    
                    if (path == null)
                    {
                        Console.WriteLine($"ERROR: Path not found for spawn point {spawnPoint.Id} with pathId={spawnPoint.PathId}");
                    }
                    
                    if (path != null)
                    {
                        Enemy enemy = null;
                        
                        // Проверяем, есть ли строковый ID врага (загружен из уровня)
                        Wave currentWave = _waves[_currentWaveIndex];
                        if (currentWave.EnemyStringIds.TryGetValue(enemyType, out string enemyStringId))
                        {
                            Console.WriteLine($"Using factory to create enemy: {enemyStringId}");
                            // Используем фабрику для создания врага по строковому ID
                            enemy = EnemyTypeFactory.Instance.CreateEnemy(enemyStringId, spawnPoint.Position, path);
                        }
                        else
                        {
                            // Старый метод через рефлексию (для совместимости)
                            IEnemyType newEnemyType = null;
                            
                            var constructor = enemyType.GetConstructor(new[] { typeof(Texture2D) });
                            if (constructor != null)
                            {
                                newEnemyType = (IEnemyType)constructor.Invoke(new object[] { _enemyTexture });
                            }
                            else
                            {
                                var defaultConstructor = enemyType.GetConstructor(System.Type.EmptyTypes);
                                if (defaultConstructor != null)
                                {
                                    newEnemyType = (IEnemyType)defaultConstructor.Invoke(null);
                                }
                            }
                            
                            if (newEnemyType != null)
                            {
                                enemy = new Enemy(newEnemyType, spawnPoint.Position, path);
                            }
                        }
                        
                        if (enemy != null)
                        {
                            _enemyController.AddEnemy(enemy);
                        }
                        
                        _remainingEnemies[enemyType]--;
                    }
                    return; // Спавним по одному врагу за раз
                }
            }
        }

        public bool IsWaveActive => _waveActive;
        public int CurrentWaveIndex => _currentWaveIndex;
        public int TotalWaves => _waves.Count;
    }
}