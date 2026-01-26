using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using SimulationEngine.MapRelated;
using System.Collections.Generic;
using EditorEngine.Waves;
using System;
using EditorEngine.UI;
using SimulationEngine.TowerRelated;

namespace EditorEngine;

public class LevelEditor
{
    private TowerEditorPanel towerPanel;
    private UITextField levelNameField;
    private MoneyHealthEditor moneyHealthPanel;

    private bool debugToggleMessage = false;
    private string statusMessage = "";
    private float debugMessageTimer = 0f;

    private WaveSet waveSet;
    private ManageWavesPanel wavesPanel;
    private EnemySelectionPanel enemySelectionPanel;

    private GameMap currentMap;
    private Camera camera;
    private const float GridSize = 32f;

    // Input states
    private KeyboardState previousKeyboardState;
    private MouseState previousMouseState;
    private SpriteFont defaultFont;

    // Editor modes
    private enum EditorMode
    {
        None,
        PlacingSpawn,
        PlacingDefense,
        DrawingPath,
        PlacingBuildZone,
        WavesEditing,

        TowerEdititing,
    }

    private EditorMode currentMode = EditorMode.None;
    private List<Vector2> currentPathPoints = new List<Vector2>();
    private string selectedPathDefensePointId = null;

    public LevelEditor(ContentManager content, int screenWidth, int screenHeight)
    {
        towerPanel = new TowerEditorPanel();
        levelNameField = new UITextField(new Rectangle(320, 10, 150, 30), "level_1");
        moneyHealthPanel = new MoneyHealthEditor();

        // Фиксированный размер карты
        currentMap = new GameMap("level_1", "Level 1", 3000, 2000);
        waveSet = new WaveSet { MapId = currentMap.Id };
        wavesPanel = new ManageWavesPanel(waveSet, currentMap);
        
        // Инициализация панели выбора врагов
        enemySelectionPanel = new EnemySelectionPanel((enemyTypeId, spawnPointId, count) => {
            wavesPanel.AddEnemySpawnToSelectedWave(enemyTypeId, spawnPointId, count);
        });
        
        camera = new Camera(new Vector2(3000, 2000), screenWidth, screenHeight);

        // Загрузка шрифта
        defaultFont = content.Load<SpriteFont>("DefaultFont"); 
    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
    {   
        moneyHealthPanel.Update(mouseState, keyboardState);

        if (keyboardState.IsKeyDown(Keys.T) && previousKeyboardState.IsKeyUp(Keys.T)){
            towerPanel.Toggle();
        }    

        if (keyboardState.IsKeyDown(Keys.M) && previousKeyboardState.IsKeyUp(Keys.M)) {
            wavesPanel.Toggle();
            currentMode = EditorMode.WavesEditing;
            debugToggleMessage = true;
            debugMessageTimer = 2f;
        }
        if (wavesPanel.IsOpen)
        {
            if (keyboardState.IsKeyDown(Keys.N) &&
                previousKeyboardState.IsKeyUp(Keys.N))
            {
                wavesPanel.AddWave();
            }

            if (keyboardState.IsKeyDown(Keys.D) &&
                previousKeyboardState.IsKeyUp(Keys.D))
            {
                wavesPanel.RemoveWave(wavesPanel.SelectedWaveIndex);
            }

            if (keyboardState.IsKeyDown(Keys.Up) &&
                previousKeyboardState.IsKeyUp(Keys.Up))
                wavesPanel.SelectWave(wavesPanel.SelectedWaveIndex - 1);

            if (keyboardState.IsKeyDown(Keys.Down) &&
                previousKeyboardState.IsKeyUp(Keys.Down))
                wavesPanel.SelectWave(wavesPanel.SelectedWaveIndex + 1);

            // Открыть панель добавления врага (клавиша E)
            if (keyboardState.IsKeyDown(Keys.E) &&
                previousKeyboardState.IsKeyUp(Keys.E) &&
                wavesPanel.SelectedWaveIndex >= 0)
            {
                enemySelectionPanel.Open();
            }
        }

        // Logical fix: moved mode selection to keyboard controls section
        // Logical fix: allowed user to toggle modes off by pressing the same key again
        if (keyboardState.IsKeyDown(Keys.D1) && previousKeyboardState.IsKeyUp(Keys.D1))
            if (currentMode != EditorMode.PlacingSpawn)
                currentMode = EditorMode.PlacingSpawn;
            else 
                currentMode = EditorMode.None;
        else if (keyboardState.IsKeyDown(Keys.D2) && previousKeyboardState.IsKeyUp(Keys.D2))
            if (currentMode != EditorMode.PlacingDefense)
                currentMode = EditorMode.PlacingDefense;
            else 
                currentMode = EditorMode.None;
        else if (keyboardState.IsKeyDown(Keys.D3) && previousKeyboardState.IsKeyUp(Keys.D3))
            if (currentMode != EditorMode.DrawingPath)
                currentMode = EditorMode.DrawingPath;
            else 
                currentMode = EditorMode.None;
        else if (keyboardState.IsKeyDown(Keys.D4) && previousKeyboardState.IsKeyUp(Keys.D4))
            if (currentMode != EditorMode.PlacingBuildZone)
                currentMode = EditorMode.PlacingBuildZone;
            else
                currentMode = EditorMode.None;

        // Save on Ctrl+S
        if (keyboardState.IsKeyDown(Keys.LeftControl) && 
            keyboardState.IsKeyDown(Keys.S) && 
            previousKeyboardState.IsKeyUp(Keys.S))
        {
            SaveAll();
        }

        // Pack level on Ctrl+P
        if (keyboardState.IsKeyDown(Keys.LeftControl) && 
            keyboardState.IsKeyDown(Keys.P) && 
            previousKeyboardState.IsKeyUp(Keys.P))
        {
            PackLevel();
        }

        camera.Update(gameTime, keyboardState, mouseState);
        HandleMouseInput(mouseState);
        
        levelNameField.Update(mouseState, keyboardState);

        // Обновляем панель башен
        if (towerPanel.IsOpen)
        {
            towerPanel.Update(mouseState, keyboardState, previousMouseState);
        }

        previousKeyboardState = keyboardState;
        previousMouseState = mouseState;
        if (debugToggleMessage)
        {
            debugMessageTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (debugMessageTimer <= 0f)
                debugToggleMessage = false;
        }
    }

    private void HandleMouseInput(MouseState mouseState)
    {
        // Если панель редактора башен открыта и мышь над ней - игнорируем ввод для карты
        if (towerPanel.IsOpen && towerPanel.GetBounds().Contains(mouseState.Position))
        {
            return;
        }

        // Toolbar buttons click handling
        int toolbarX = 10;
        int toolbarY = 10;
        int buttonSize = 40;
        int buttonSpacing = 10;

        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            // Spawn point button
            if (new Rectangle(toolbarX, toolbarY, buttonSize, buttonSize).Contains(mouseState.Position))
            {
                currentMode = EditorMode.PlacingSpawn;
                return;
            }
            // Defense point button
            if (new Rectangle(toolbarX + buttonSize + buttonSpacing, toolbarY, buttonSize, buttonSize).Contains(mouseState.Position))
            {
                currentMode = EditorMode.PlacingDefense;
                return;
            }
            // Path drawing button
            if (new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 2, toolbarY, buttonSize, buttonSize).Contains(mouseState.Position))
            {
                currentMode = EditorMode.DrawingPath;
                return;
            }
            // Build zone button
            if (new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 3, toolbarY, buttonSize, buttonSize).Contains(mouseState.Position))
            {
                currentMode = EditorMode.PlacingBuildZone;
                return;
            }
            // Save button
            if (new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 4, toolbarY, buttonSize, buttonSize).Contains(mouseState.Position))
            {
                SaveAll();
                return;
            }
            // Pack button
            if (new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 5, toolbarY, buttonSize, buttonSize).Contains(mouseState.Position))
            {
                PackLevel();
                return;
            }
        }

        Vector2 worldMousePos = camera.ScreenToWorld(mouseState.Position.ToVector2());

        // Обработка клика по панели выбора врагов
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            if (enemySelectionPanel.IsOpen)
            {
                enemySelectionPanel.HandleClick(mouseState.Position, 
                    wavesPanel.GetAllEnemyConfigs(), 
                    wavesPanel.GetAvailableSpawnPointIds());
                return;
            }

            // Обработка клика по панели волн для выбора волны
            if (wavesPanel.IsOpen)
            {
                Rectangle panelRect = new Rectangle(50, 50, 450, 550);
                if (panelRect.Contains(mouseState.Position))
                {
                    int y = panelRect.Y + 50;
                    var waves = wavesPanel.GetWaves();
                    for (int i = 0; i < waves.Count; i++)
                    {
                        Rectangle waveRect = new Rectangle(panelRect.X + 10, y, panelRect.Width - 20, 30);
                        if (waveRect.Contains(mouseState.Position))
                        {
                            wavesPanel.SelectWave(i);
                            return;
                        }
                        y += 35;
                    }
                }
            }
        }

        // Left click actions
        if (mouseState.LeftButton == ButtonState.Pressed && previousMouseState.LeftButton == ButtonState.Released)
        {
            switch (currentMode)
            {
                case EditorMode.PlacingSpawn:
                    PlaceSpawnPoint(worldMousePos);
                    break;
                case EditorMode.PlacingDefense:
                    PlaceDefensePoint(worldMousePos);
                    break;
                case EditorMode.DrawingPath:
                    AddPathPoint(worldMousePos);
                    break;
                case EditorMode.PlacingBuildZone:
                    PlaceBuildZone(worldMousePos);
                    break;
            }
        }

        // Right click to finalize path
        if (mouseState.RightButton == ButtonState.Pressed && previousMouseState.RightButton == ButtonState.Released)
        {
            if (currentMode == EditorMode.DrawingPath)
            {
                FinalizePath();
            }
        }

        // Delete on backspace
        if (previousKeyboardState.IsKeyDown(Keys.Back) && !previousKeyboardState.IsKeyDown(Keys.Back))
        {
            // Could implement deletion of last element
        }
    }

    private void PlaceSpawnPoint(Vector2 position)
    {
        var spawnPoint = new SpawnPoint(position, $"spawn_{currentMap.SpawnPoints.Count}", "");
        currentMap.AddSpawnPoint(spawnPoint);
    }

    private void PlaceDefensePoint(Vector2 position)
    {
        var defensePoint = new DefensePoint(position, $"defense_{currentMap.DefensePoints.Count}", 100);
        currentMap.AddDefensePoint(defensePoint);
        selectedPathDefensePointId = defensePoint.Id;
    }

    private void PlaceBuildZone(Vector2 position)
    {
        var buildZone = new BuildZone(position, $"build_{currentMap.BuildZones.Count}");
        currentMap.AddBuildZone(buildZone);
    }

    private void AddPathPoint(Vector2 position)
    {
        currentPathPoints.Add(position);
    }

    private void FinalizePath()
    {
        if (currentPathPoints.Count < 2 || selectedPathDefensePointId == null)
        {
            currentPathPoints.Clear();
            return;
        }

        var path = new Path($"path_{currentMap.Paths.Count}", selectedPathDefensePointId, true, 20);
        foreach (var point in currentPathPoints)
        {
            path.AddWaypoint(point);
        }

        currentMap.AddPath(path);
        currentPathPoints.Clear();
        currentMode = EditorMode.None;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        DrawGrid(spriteBatch, pixel);
        DrawMapBoundaries(spriteBatch, pixel);
        DrawSpawnPoints(spriteBatch, pixel);
        DrawDefensePoints(spriteBatch, pixel);
        DrawBuildZones(spriteBatch, pixel);
        DrawPaths(spriteBatch, pixel);
        DrawCurrentPath(spriteBatch, pixel);
        DrawUI(spriteBatch, pixel);


        moneyHealthPanel.Draw(spriteBatch, defaultFont, pixel);

        if (towerPanel.IsOpen)
            towerPanel.Draw(spriteBatch, defaultFont, pixel);

        if (wavesPanel.IsOpen)
            DrawWavesPanel(spriteBatch, pixel);

        if (enemySelectionPanel.IsOpen)
            enemySelectionPanel.Draw(spriteBatch, defaultFont, pixel, 
                wavesPanel.GetAllEnemyConfigs(), 
                wavesPanel.GetAvailableSpawnPointIds());

    }

    private void DrawGrid(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Color gridColor = Color.DarkGray * 0.3f;
        
        int screenWidth = spriteBatch.GraphicsDevice.Viewport.Width;
        int screenHeight = spriteBatch.GraphicsDevice.Viewport.Height;

        // Сетка в экранных координатах - не двигается
        for (float x = 0; x < screenWidth; x += GridSize)
        {
            DrawLine(spriteBatch, pixel, new Vector2(x, 0), new Vector2(x, screenHeight), gridColor);
        }

        for (float y = 0; y < screenHeight; y += GridSize)
        {
            DrawLine(spriteBatch, pixel, new Vector2(0, y), new Vector2(screenWidth, y), gridColor);
        }
    }

    private void DrawMapBoundaries(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Vector2 topLeft = camera.WorldToScreen(Vector2.Zero);
        Vector2 topRight = camera.WorldToScreen(new Vector2(currentMap.Width, 0));
        Vector2 bottomLeft = camera.WorldToScreen(new Vector2(0, currentMap.Height));
        Vector2 bottomRight = camera.WorldToScreen(new Vector2(currentMap.Width, currentMap.Height));

        Color boundaryColor = Color.White;
        int thickness = 3;

        // Draw four edges
        DrawLine(spriteBatch, pixel, topLeft, topRight, boundaryColor, thickness);
        DrawLine(spriteBatch, pixel, topRight, bottomRight, boundaryColor, thickness);
        DrawLine(spriteBatch, pixel, bottomRight, bottomLeft, boundaryColor, thickness);
        DrawLine(spriteBatch, pixel, bottomLeft, topLeft, boundaryColor, thickness);
    }

    private void DrawSpawnPoints(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var spawn in currentMap.SpawnPoints)
        {
            var screenPos = camera.WorldToScreen(spawn.Position);
            DrawCircle(spriteBatch, pixel, screenPos, 10 * camera.Zoom, Color.Green);
            DrawCircleOutline(spriteBatch, pixel, screenPos, 10, Color.LimeGreen, 2);
        }
    }

    private void DrawDefensePoints(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var defense in currentMap.DefensePoints)
        {
            var screenPos = camera.WorldToScreen(defense.Position);
            DrawCircle(spriteBatch, pixel, screenPos, 15 * camera.Zoom, Color.Red);
            DrawCircleOutline(spriteBatch, pixel, screenPos, 15 * camera.Zoom, Color.IndianRed, 2);
        }
    }

    private void DrawBuildZones(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var buildZone in currentMap.BuildZones)
        {
            var screenPos = camera.WorldToScreen(buildZone.Position);
            var size = buildZone.Size * camera.Zoom;
            
            // Рисуем прямоугольник зоны
            Rectangle rect = new Rectangle(
                (int)(screenPos.X - size.X / 2),
                (int)(screenPos.Y - size.Y / 2),
                (int)size.X,
                (int)size.Y
            );
            
            // Заливка
            spriteBatch.Draw(pixel, rect, Color.Blue * 0.3f);
            
            // Рамка
            DrawRectangleOutline(spriteBatch, pixel, rect, Color.Cyan, 2);
        }
    }

    private void DrawPaths(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var path in currentMap.Paths)
        {
            var smoothPath = path.GetSmoothPath();
            for (int i = 0; i < smoothPath.Count - 1; i++)
            {
                var start = camera.WorldToScreen(smoothPath[i]);
                var end = camera.WorldToScreen(smoothPath[i + 1]);
                DrawLine(spriteBatch, pixel, start, end, Color.Yellow, (int)(2 * camera.Zoom));
            }

            // Draw waypoints
            foreach (var waypoint in path.Waypoints)
            {
                var screenPos = camera.WorldToScreen(waypoint);
                DrawCircle(spriteBatch, pixel, screenPos, 5 * camera.Zoom, Color.Orange);
            }
        }
    }

    private void DrawCurrentPath(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (currentMode != EditorMode.DrawingPath || currentPathPoints.Count == 0)
            return;

        if (currentPathPoints.Count >= 2)
        {
            // Создаем временный путь для предпросмотра сплайна
            var tempPath = new SimulationEngine.MapRelated.Path("temp", "", true, 20);
            foreach (var p in currentPathPoints) tempPath.AddWaypoint(p);
            
            var smoothPoints = tempPath.GetSmoothPath();
            for (int i = 0; i < smoothPoints.Count - 1; i++)
            {
                var start = camera.WorldToScreen(smoothPoints[i]);
                var end = camera.WorldToScreen(smoothPoints[i + 1]);
                DrawLine(spriteBatch, pixel, start, end, Color.Cyan * 0.7f, (int)(2 * camera.Zoom));
            }
        }

        // Отрисовка контрольных точек (узлов)
        foreach (var point in currentPathPoints)
        {
            var screenPos = camera.WorldToScreen(point);
            DrawCircle(spriteBatch, pixel, screenPos, 6 * camera.Zoom, Color.Cyan);
        }
    }

    private void DrawWavesPanel(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Панель фиксирована на экране, координаты не зависят от камеры
        Rectangle panelRect = new Rectangle(50, 50, 450, 550);

        // Фон панели
        spriteBatch.Draw(pixel, panelRect, Color.Black * 0.85f);

        // Заголовок
        DrawRectangle(spriteBatch, pixel,
            new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 40),
            Color.DarkSlateGray);
        spriteBatch.DrawString(defaultFont, "Wave Manager",
            new Vector2(panelRect.X + 10, panelRect.Y + 10), Color.White);

        // Список волн
        int y = panelRect.Y + 50;
        var waves = wavesPanel.GetWaves();
        for (int i = 0; i < waves.Count; i++)
        {
            Color c = i == wavesPanel.SelectedWaveIndex ? Color.Cyan : Color.Gray;
            Rectangle waveRect = new Rectangle(panelRect.X + 10, y, panelRect.Width - 20, 30);
            DrawRectangle(spriteBatch, pixel, waveRect, c * 0.5f);

            // Название волны и количество врагов
            int enemyCount = waves[i].Spawns.Count;
            spriteBatch.DrawString(defaultFont, $"Wave {i + 1} ({enemyCount} spawns)", 
                new Vector2(waveRect.X + 6, waveRect.Y + 6), Color.White);

            y += 35;
        }

        y += 10;

        // Показываем детали выбранной волны
        if (wavesPanel.SelectedWaveIndex >= 0)
        {
            var selectedWave = wavesPanel.GetSelectedWave();
            if (selectedWave != null)
            {
                spriteBatch.DrawString(defaultFont, "Enemies in wave:",
                    new Vector2(panelRect.X + 10, y), Color.Yellow);
                y += 25;

                var spawns = selectedWave.Spawns;
                if (spawns.Count == 0)
                {
                    spriteBatch.DrawString(defaultFont, "No enemies yet. Press E to add.",
                        new Vector2(panelRect.X + 10, y), Color.Gray);
                }
                else
                {
                    foreach (var spawn in spawns)
                    {
                        var enemyInfo = wavesPanel.GetEnemyTypeInfo(spawn.EnemyTypeId);
                        string enemyName = enemyInfo?.DisplayName ?? spawn.EnemyTypeId;
                        string text = $"{enemyName} x{spawn.Count} @ {spawn.SpawnPointId}";
                        spriteBatch.DrawString(defaultFont, text,
                            new Vector2(panelRect.X + 15, y), Color.White);
                        y += 20;
                    }
                }
            }

            y += 10;
            spriteBatch.DrawString(defaultFont, "Controls:",
                new Vector2(panelRect.X + 10, y), Color.LightGreen);
            y += 20;
            spriteBatch.DrawString(defaultFont, "N - Add Wave",
                new Vector2(panelRect.X + 10, y), Color.White);
            y += 18;
            spriteBatch.DrawString(defaultFont, "D - Delete Wave",
                new Vector2(panelRect.X + 10, y), Color.White);
            y += 18;
            spriteBatch.DrawString(defaultFont, "E - Add Enemy",
                new Vector2(panelRect.X + 10, y), Color.White);
            y += 18;
            spriteBatch.DrawString(defaultFont, "Up/Down - Select Wave",
                new Vector2(panelRect.X + 10, y), Color.White);
        }
    }

    private void DrawUI(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // --- Toolbar (кнопки режимов редактора) ---
        int toolbarX = 10;
        int toolbarY = 10;
        int buttonSize = 40;
        int buttonSpacing = 10;

        DrawRectangle(spriteBatch, pixel, new Rectangle(toolbarX - 5, toolbarY - 5, 
            (buttonSize + buttonSpacing) * 5 + buttonSize + 10 + 170, buttonSize + 10), Color.Black * 0.7f);

        // Spawn Point
        Color spawnColor = currentMode == EditorMode.PlacingSpawn ? Color.LimeGreen : Color.Gray;
        DrawRectangle(spriteBatch, pixel, new Rectangle(toolbarX, toolbarY, buttonSize, buttonSize), spawnColor);
        DrawCircle(spriteBatch, pixel, new Vector2(toolbarX + buttonSize / 2, toolbarY + buttonSize / 2), 8, Color.Green);

        // Defense Point
        Color defenseColor = currentMode == EditorMode.PlacingDefense ? Color.IndianRed : Color.Gray;
        DrawRectangle(spriteBatch, pixel, new Rectangle(toolbarX + buttonSize + buttonSpacing, toolbarY, buttonSize, buttonSize), defenseColor);
        DrawCircle(spriteBatch, pixel, new Vector2(toolbarX + buttonSize + buttonSpacing + buttonSize / 2, toolbarY + buttonSize / 2), 10, Color.Red);

        // Path Drawing
        Color pathColor = currentMode == EditorMode.DrawingPath ? Color.Yellow : Color.Gray;
        DrawRectangle(spriteBatch, pixel, new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 2, toolbarY, buttonSize, buttonSize), pathColor);
        DrawLine(spriteBatch, pixel, new Vector2(toolbarX + (buttonSize + buttonSpacing) * 2 + 10, toolbarY + buttonSize / 2),
            new Vector2(toolbarX + (buttonSize + buttonSpacing) * 2 + buttonSize - 10, toolbarY + buttonSize / 2), Color.Orange, 3);

        // Build Zone button
        Color buildColor = currentMode == EditorMode.PlacingBuildZone ? Color.Cyan : Color.Gray;
        DrawRectangle(spriteBatch, pixel, new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 3, toolbarY, buttonSize, buttonSize), buildColor);
        Rectangle buildRect = new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 3 + 10, toolbarY + 10, 20, 20);
        DrawRectangle(spriteBatch, pixel, buildRect, Color.Blue * 0.5f);

        // Save button
        DrawRectangle(spriteBatch, pixel, new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 4, toolbarY, buttonSize, buttonSize), Color.Gray);
        spriteBatch.DrawString(defaultFont, "S", new Vector2(toolbarX + (buttonSize + buttonSpacing) * 4 + 12, toolbarY + 8), Color.White);

        // Pack button
        DrawRectangle(spriteBatch, pixel, new Rectangle(toolbarX + (buttonSize + buttonSpacing) * 5, toolbarY, buttonSize, buttonSize), Color.DarkGoldenrod);
        spriteBatch.DrawString(defaultFont, "P", new Vector2(toolbarX + (buttonSize + buttonSpacing) * 5 + 12, toolbarY + 8), Color.White);

        // Level Name Field
        levelNameField.Draw(spriteBatch, defaultFont, pixel);
        spriteBatch.DrawString(defaultFont, "Level name:", new Vector2(320, 42), Color.White * 0.8f);

        // --- Status Panel ---
        int infoY = toolbarY + buttonSize + 20;
        DrawRectangle(spriteBatch, pixel, new Rectangle(10, infoY, 300, 160), Color.Black * 0.7f);
        spriteBatch.DrawString(defaultFont, "Spawns: " + currentMap.SpawnPoints.Count, new Vector2(15, infoY + 5), Color.LimeGreen);
        spriteBatch.DrawString(defaultFont, "Defense: " + currentMap.DefensePoints.Count, new Vector2(15, infoY + 25), Color.IndianRed);
        spriteBatch.DrawString(defaultFont, "Paths: " + currentMap.Paths.Count, new Vector2(15, infoY + 45), Color.Yellow);
        spriteBatch.DrawString(defaultFont, "Build Zones: " + currentMap.BuildZones.Count, new Vector2(15, infoY + 65), Color.Cyan);
        spriteBatch.DrawString(defaultFont, "Map ID: " + currentMap.Id, new Vector2(15, infoY + 85), Color.White);
        spriteBatch.DrawString(defaultFont, "Hotkeys: Ctrl+S (Save), Ctrl+P (Pack)", new Vector2(15, infoY + 115), Color.LightGray);
        spriteBatch.DrawString(defaultFont, "        T (Towers), M (Waves)", new Vector2(15, infoY + 135), Color.LightGray);



        if (currentMode == EditorMode.DrawingPath && currentPathPoints.Count > 0)
        {
            spriteBatch.DrawString(defaultFont, $"Points: {currentPathPoints.Count}", 
                                new Vector2(15, infoY + 85), Color.Cyan);
        }

        if (debugToggleMessage)
        {
            spriteBatch.DrawString(defaultFont, statusMessage, new Vector2(toolbarX + (buttonSize + buttonSpacing) * 6 + 10, toolbarY + 10), Color.Yellow);
        }
    }

    private void DrawRectangle(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, rect, color);
    }

    private void DrawRectangleOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        // Верх
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Низ
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Лево
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Право
        spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }

    public void SaveAll()
    {
        string levelId = levelNameField.Text.Trim();
        if (string.IsNullOrEmpty(levelId)) levelId = "level_1";
        
        currentMap.Id = levelId;
        currentMap.Name = levelId;
        waveSet.MapId = levelId;

        MapSerializer.SaveMap(currentMap, $"Content/Maps/{currentMap.Id}.json");
        WaveSerializer.Save(waveSet, $"Content/Maps/{currentMap.Id}.waves.json");
        Console.WriteLine($"Level {currentMap.Id} saved successfully!");
        statusMessage = $"Level {currentMap.Id} saved!";
        debugToggleMessage = true;
        debugMessageTimer = 3f;
    }

    public void PackLevel()
    {
        try
        {
            string levelId = levelNameField.Text.Trim();
            if (string.IsNullOrEmpty(levelId)) levelId = "level_1";

            currentMap.Id = levelId;
            currentMap.Name = levelId;
            waveSet.MapId = levelId;

            string outputPath = $"Content/{currentMap.Id}.zip";
            LevelPackager.PackLevel(currentMap, waveSet, outputPath);
            Console.WriteLine($"Level packed successfully to: {outputPath}");
            statusMessage = $"Level packed to {currentMap.Id}.zip!";
            debugToggleMessage = true;
            debugMessageTimer = 3f;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error packing level: {ex.Message}");
            statusMessage = "Error packing level!";
            debugToggleMessage = true;
            debugMessageTimer = 3f;
        }
    }

    public void LoadAll(string mapId)
    {
        currentMap = MapSerializer.LoadMap($"Content/Maps/{mapId}.json");
        waveSet = WaveSerializer.Load($"Content/Maps/{mapId}.waves.json")
                ?? new WaveSet { MapId = mapId };
    }


    // Helper drawing methods
    private void DrawCircle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius, Color color)
    {
        for (float angle = 0; angle < 6.28f; angle += 0.1f)
        {
            float x = center.X + radius * (float)System.Math.Cos(angle);
            float y = center.Y + radius * (float)System.Math.Sin(angle);
            spriteBatch.Draw(pixel, new Vector2(x, y), color);
        }
    }

    private void DrawCircleOutline(SpriteBatch spriteBatch, Texture2D pixel, Vector2 center, float radius, Color color, int thickness)
    {
        for (int i = 0; i < thickness; i++)
        {
            DrawCircle(spriteBatch, pixel, center, radius - i, color);
        }
    }

    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness = 1)
    {
        float distance = Vector2.Distance(start, end);
        float angle = (float)System.Math.Atan2(end.Y - start.Y, end.X - start.X);

        spriteBatch.Draw(pixel, start, null, color, angle, Vector2.Zero, 
            new Vector2(distance, thickness), SpriteEffects.None, 0);
    }
}
