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

    private bool debugToggleMessage = false;
    private float debugMessageTimer = 0f;

    private WaveSet waveSet;
    private ManageWavesPanel wavesPanel;

    private GameMap currentMap;
    private Vector2 cameraPosition = Vector2.Zero;
    private const float CameraSpeed = 300f;
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
        WavesEditing,

        TowerEdititing,
    }

    private EditorMode currentMode = EditorMode.None;
    private List<Vector2> currentPathPoints = new List<Vector2>();
    private string selectedPathDefensePointId = null;

    public LevelEditor(ContentManager content)
    {
        towerPanel = new TowerEditorPanel(new TowerConfig());

        currentMap = new GameMap("level_1", "Level 1", 1920, 1080);
        waveSet = new WaveSet { MapId = currentMap.Id };
        wavesPanel = new ManageWavesPanel(waveSet, currentMap);

            // Загрузка шрифта
        defaultFont = content.Load<SpriteFont>("DefaultFont"); 
        
        // Авто-показ панели при старте

    }

    public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
    {
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

            if (keyboardState.IsKeyDown(Keys.Up))
                wavesPanel.SelectWave(wavesPanel.SelectedWaveIndex - 1);

            if (keyboardState.IsKeyDown(Keys.Down))
                wavesPanel.SelectWave(wavesPanel.SelectedWaveIndex + 1);
        }

        HandleCameraMovement(gameTime, keyboardState);
        HandleMouseInput(mouseState);


        previousKeyboardState = keyboardState;
        previousMouseState = mouseState;
        if (debugToggleMessage)
        {
            debugMessageTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (debugMessageTimer <= 0f)
                debugToggleMessage = false;
        }
    }

    private void HandleCameraMovement(GameTime gameTime, KeyboardState keyboardState)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (keyboardState.IsKeyDown(Keys.W)) cameraPosition.Y -= CameraSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.S)) cameraPosition.Y += CameraSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.A)) cameraPosition.X -= CameraSpeed * deltaTime;
        if (keyboardState.IsKeyDown(Keys.D)) cameraPosition.X += CameraSpeed * deltaTime;
    }

    private void HandleMouseInput(MouseState mouseState)
    {
        Vector2 worldMousePos = ScreenToWorldSpace(mouseState.Position.ToVector2());

        // Mode selection
        if (previousKeyboardState.IsKeyDown(Keys.D1) && !previousMouseState.LeftButton.Equals(ButtonState.Pressed))
            currentMode = EditorMode.PlacingSpawn;
        else if (previousKeyboardState.IsKeyDown(Keys.D2) && !previousMouseState.LeftButton.Equals(ButtonState.Pressed))
            currentMode = EditorMode.PlacingDefense;
        else if (previousKeyboardState.IsKeyDown(Keys.D3) && !previousMouseState.LeftButton.Equals(ButtonState.Pressed))
            currentMode = EditorMode.DrawingPath;

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

        // Save on Ctrl+S
        if (previousKeyboardState.IsKeyDown(Keys.LeftControl) && previousKeyboardState.IsKeyDown(Keys.S))
        {
            MapSerializer.SaveMap(currentMap, $"Content/Maps/{currentMap.Id}.json");
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
        DrawPaths(spriteBatch, pixel);
        DrawCurrentPath(spriteBatch, pixel);
        DrawUI(spriteBatch, pixel);

        if (towerPanel.IsOpen)
            towerPanel.Draw(spriteBatch, defaultFont, pixel);

        if (wavesPanel.IsOpen)
            DrawWavesPanel(spriteBatch, pixel);

    }

    private void DrawGrid(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Color gridColor = Color.DarkGray * 0.3f;

        for (float x = -cameraPosition.X % GridSize; x < 1920; x += GridSize)
        {
            DrawLine(spriteBatch, pixel, new Vector2(x, 0), new Vector2(x, 1080), gridColor);
        }

        for (float y = -cameraPosition.Y % GridSize; y < 1080; y += GridSize)
        {
            DrawLine(spriteBatch, pixel, new Vector2(0, y), new Vector2(1920, y), gridColor);
        }
    }

    private void DrawMapBoundaries(SpriteBatch spriteBatch, Texture2D pixel)
    {
        Vector2 topLeft = WorldToScreenSpace(Vector2.Zero);
        Vector2 topRight = WorldToScreenSpace(new Vector2(currentMap.Width, 0));
        Vector2 bottomLeft = WorldToScreenSpace(new Vector2(0, currentMap.Height));
        Vector2 bottomRight = WorldToScreenSpace(new Vector2(currentMap.Width, currentMap.Height));

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
            var screenPos = WorldToScreenSpace(spawn.Position);
            DrawCircle(spriteBatch, pixel, screenPos, 10, Color.Green);
            DrawCircleOutline(spriteBatch, pixel, screenPos, 10, Color.LimeGreen, 2);
        }
    }

    private void DrawDefensePoints(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var defense in currentMap.DefensePoints)
        {
            var screenPos = WorldToScreenSpace(defense.Position);
            DrawCircle(spriteBatch, pixel, screenPos, 15, Color.Red);
            DrawCircleOutline(spriteBatch, pixel, screenPos, 15, Color.IndianRed, 2);
        }
    }

    private void DrawPaths(SpriteBatch spriteBatch, Texture2D pixel)
    {
        foreach (var path in currentMap.Paths)
        {
            var smoothPath = path.GetSmoothPath();
            for (int i = 0; i < smoothPath.Count - 1; i++)
            {
                var start = WorldToScreenSpace(smoothPath[i]);
                var end = WorldToScreenSpace(smoothPath[i + 1]);
                DrawLine(spriteBatch, pixel, start, end, Color.Yellow, 2);
            }

            // Draw waypoints
            foreach (var waypoint in path.Waypoints)
            {
                var screenPos = WorldToScreenSpace(waypoint);
                DrawCircle(spriteBatch, pixel, screenPos, 5, Color.Orange);
            }
        }
    }

    private void DrawCurrentPath(SpriteBatch spriteBatch, Texture2D pixel)
    {
        if (currentMode != EditorMode.DrawingPath || currentPathPoints.Count == 0)
            return;

        // Draw lines between points
        for (int i = 0; i < currentPathPoints.Count - 1; i++)
        {
            var start = WorldToScreenSpace(currentPathPoints[i]);
            var end = WorldToScreenSpace(currentPathPoints[i + 1]);
            DrawLine(spriteBatch, pixel, start, end, Color.Cyan, 2);
        }

        // Draw points
        foreach (var point in currentPathPoints)
        {
            var screenPos = WorldToScreenSpace(point);
            DrawCircle(spriteBatch, pixel, screenPos, 6, Color.Cyan);
        }
    }

    private void DrawWavesPanel(SpriteBatch spriteBatch, Texture2D pixel)
    {
        // Панель фиксирована на экране, координаты не зависят от камеры
        Rectangle panelRect = new Rectangle(50, 50, 400, 500);

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

            // Название волны (или индекс)
            spriteBatch.DrawString(defaultFont, $"Wave {i + 1}", new Vector2(waveRect.X + 6, waveRect.Y + 6), Color.White);

            y += 35;
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
            (buttonSize + buttonSpacing) * 3 + 5, buttonSize + 10), Color.Black * 0.7f);

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

        // --- Status Panel ---
        int infoY = toolbarY + buttonSize + 20;
        DrawRectangle(spriteBatch, pixel, new Rectangle(10, infoY, 300, 100), Color.Black * 0.7f);
        spriteBatch.DrawString(defaultFont, "Spawns: " + currentMap.SpawnPoints.Count, new Vector2(15, 55), Color.LimeGreen);
        spriteBatch.DrawString(defaultFont, "Defense: " + currentMap.DefensePoints.Count, new Vector2(15, 75), Color.IndianRed);
        spriteBatch.DrawString(defaultFont, "Paths: " + currentMap.Paths.Count, new Vector2(15, 95), Color.Yellow);


        if (currentMode == EditorMode.DrawingPath && currentPathPoints.Count > 0)
        {
            spriteBatch.DrawString(defaultFont, $"Points: {currentPathPoints.Count}", 
                                new Vector2(15, infoY + 65), Color.Cyan);
        }

    }

    private void DrawRectangle(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, rect, color);
    }

    public void SaveAll()
    {
        MapSerializer.SaveMap(currentMap, $"Content/Maps/{currentMap.Id}.json");
        WaveSerializer.Save(waveSet, $"Content/Maps/{currentMap.Id}.waves.json");
    }

    public void LoadAll(string mapId)
    {
        currentMap = MapSerializer.LoadMap($"Content/Maps/{mapId}.json");
        waveSet = WaveSerializer.Load($"Content/Maps/{mapId}.waves.json")
                ?? new WaveSet { MapId = mapId };
    }


    private Vector2 ScreenToWorldSpace(Vector2 screenPos) => screenPos + cameraPosition;
    private Vector2 WorldToScreenSpace(Vector2 worldPos) => worldPos - cameraPosition;

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
