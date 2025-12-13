using System;
using System.Collections.Generic;
using System.Linq;
using EditorEngine.Waves;
using SimulationEngine.MapRelated;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine.UI;

namespace EditorEngine.Waves;

public class ManageWavesPanel
{
    private readonly WaveSet waveSet;
    private readonly GameMap map;

    public bool IsOpen { get; private set; }

    public int SelectedWaveIndex { get; private set; } = -1;

    public ManageWavesPanel(WaveSet waveSet, GameMap map)
    {
        this.waveSet = waveSet;
        this.map = map;
    }

    // -------------------------
    // Panel visibility
    // -------------------------

    public void Open()
    {
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
        SelectedWaveIndex = -1;
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        if (!IsOpen)
            SelectedWaveIndex = -1;
    }

    // -------------------------
    // Waves
    // -------------------------

    public IReadOnlyList<Wave> GetWaves() => waveSet.Waves;

    public void AddWave()
    {
        waveSet.Waves.Add(new Wave
        {
            Index = waveSet.Waves.Count
        });
    }

    public void RemoveWave(int index)
    {
        if (index < 0 || index >= waveSet.Waves.Count)
            return;

        waveSet.Waves.RemoveAt(index);

        // Reindex waves
        for (int i = 0; i < waveSet.Waves.Count; i++)
            waveSet.Waves[i].Index = i;

        if (SelectedWaveIndex == index)
            SelectedWaveIndex = -1;
    }

    public void SelectWave(int index)
    {
        if (index < 0 || index >= waveSet.Waves.Count)
            return;

        SelectedWaveIndex = index;
    }

    public Wave GetSelectedWave()
    {
        if (SelectedWaveIndex < 0 || SelectedWaveIndex >= waveSet.Waves.Count)
            return null;

        return waveSet.Waves[SelectedWaveIndex];
    }

    // -------------------------
    // Enemy spawns
    // -------------------------

    public void AddEnemySpawnToSelectedWave(
        string enemyTypeId,
        string spawnPointId,
        int count)
    {
        var wave = GetSelectedWave();
        if (wave == null)
            return;

        wave.Spawns.Add(new EnemySpawn
        {
            EnemyTypeId = enemyTypeId,
            SpawnPointId = spawnPointId,
            Count = count
        });
    }

    public void RemoveEnemySpawnFromSelectedWave(int spawnIndex)
    {
        var wave = GetSelectedWave();
        if (wave == null)
            return;

        if (spawnIndex < 0 || spawnIndex >= wave.Spawns.Count)
            return;

        wave.Spawns.RemoveAt(spawnIndex);
    }

    public IReadOnlyList<EnemySpawn> GetEnemySpawnsOfSelectedWave()
    {
        return GetSelectedWave()?.Spawns;
    }

    // -------------------------
    // Helpers for UI
    // -------------------------

    public IReadOnlyList<string> GetAvailableSpawnPointIds()
    {
        return map.SpawnPoints.Select(s => s.Id).ToList();
    }

    /// <summary>
    /// Пока просто строковый список.
    /// Потом можно связать с EnemyRegistry / configs.
    /// </summary>
    public IReadOnlyList<string> GetAvailableEnemyTypeIds()
    {
        return new List<string>
        {
            "basic",
            "fast",
            "tank"
        };
    }
     private void DrawRectangle(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color)
    {
        spriteBatch.Draw(pixel, rect, color);
    }



    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
{
    if (!IsOpen) return;

    Rectangle panelRect = new Rectangle(50, 50, 400, 500);
    sb.Draw(pixel, panelRect, Color.Black * 0.85f);

    // Заголовок
    DrawRectangle(sb, pixel, new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 40), Color.DarkSlateGray);
    sb.DrawString(font, "Wave Manager", new Vector2(panelRect.X + 10, panelRect.Y + 10), Color.White);

    // Кнопки Add / Remove
    UIButton addWaveButton = new UIButton(new Rectangle(panelRect.X + 10, panelRect.Y + 50, 80, 30), "Add Wave", AddWave);
    UIButton removeWaveButton = new UIButton(new Rectangle(panelRect.X + 100, panelRect.Y + 50, 80, 30), "Remove Wave", () => RemoveWave(SelectedWaveIndex));
    addWaveButton.Draw(sb, font, pixel);
    removeWaveButton.Draw(sb, font, pixel);

    // Список волн
    int y = panelRect.Y + 90;
    for (int i = 0; i < waveSet.Waves.Count; i++)
    {
        Color c = i == SelectedWaveIndex ? Color.Cyan : Color.Gray;
        Rectangle waveRect = new Rectangle(panelRect.X + 10, y, panelRect.Width - 20, 30);
        DrawRectangle(sb, pixel, waveRect, c * 0.5f);
        sb.DrawString(font, $"Wave {i + 1}", new Vector2(waveRect.X + 6, waveRect.Y + 6), Color.White);
        y += 35;
    }
}

}
