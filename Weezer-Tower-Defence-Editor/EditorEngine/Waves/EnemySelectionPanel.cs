using System;
using System.Collections.Generic;
using EditorEngine.UI;
using EditorEngine.Enemies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EditorEngine.Waves;

/// <summary>
/// Панель для выбора врага и добавления его в волну
/// </summary>
public class EnemySelectionPanel
{
    private Rectangle panelRect;
    private string selectedEnemyTypeId = null;
    private string selectedSpawnPointId = null;
    private int enemyCount = 1;
    
    public bool IsOpen { get; private set; }
    
    private Action<string, string, int> onEnemyAdded;

    public EnemySelectionPanel(Action<string, string, int> onEnemyAdded)
    {
        this.onEnemyAdded = onEnemyAdded;
        panelRect = new Rectangle(500, 100, 450, 500);
    }

    public void Open()
    {
        IsOpen = true;
        selectedEnemyTypeId = null;
        selectedSpawnPointId = null;
        enemyCount = 1;
    }

    public void Close()
    {
        IsOpen = false;
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel, IReadOnlyList<string> spawnPointIds)
    {
        if (!IsOpen) return;

        // Фон панели
        sb.Draw(pixel, panelRect, Color.Black * 0.9f);
        
        // Заголовок
        Rectangle headerRect = new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, 40);
        sb.Draw(pixel, headerRect, Color.DarkSlateBlue);
        sb.DrawString(font, "Add Enemy to Wave", new Vector2(panelRect.X + 10, panelRect.Y + 10), Color.White);

        int y = panelRect.Y + 50;

        // Секция выбора типа врага
        sb.DrawString(font, "Enemy Type:", new Vector2(panelRect.X + 10, y), Color.White);
        y += 25;

        EnemyRegistry enemyRegistry = EnemyRegistry.Instance;
        Dictionary<string, TypeSpecification> enemies = enemyRegistry.enemies;

        if (enemies.Keys.Count == 0)
        {
            sb.DrawString(font, "No enemies found. Create one beforehand", new Vector2(panelRect.X + 10, y), Color.Red);
            y += 30;
        }
        else
        {
            foreach (var enemy in enemies.Keys)
            {
                Color buttonColor = selectedEnemyTypeId == enemy ? Color.Green : Color.Gray;
                Rectangle enemyRect = new Rectangle(panelRect.X + 10, y, panelRect.Width - 20, 35);
                sb.Draw(pixel, enemyRect, buttonColor * 0.6f);
                
                // Информация о враге с его поведением
                sb.DrawString(font, enemy, new Vector2(enemyRect.X + 5, enemyRect.Y + 8), Color.White);
                
                y += 40;
            }
        }

        y += 10;

        // Секция выбора точки спавна
        sb.DrawString(font, "Spawn Point:", new Vector2(panelRect.X + 10, y), Color.White);
        y += 25;

        if (spawnPointIds.Count == 0)
        {
            sb.DrawString(font, "No spawn points available!", new Vector2(panelRect.X + 10, y), Color.Red);
            y += 30;
        }
        else
        {
            foreach (var spawnId in spawnPointIds)
            {
                Color buttonColor = selectedSpawnPointId == spawnId ? Color.Green : Color.Gray;
                Rectangle spawnRect = new Rectangle(panelRect.X + 10, y, panelRect.Width - 20, 30);
                sb.Draw(pixel, spawnRect, buttonColor * 0.6f);
                sb.DrawString(font, spawnId, new Vector2(spawnRect.X + 5, spawnRect.Y + 5), Color.White);
                
                y += 35;
            }
        }

        y += 10;

        // Количество врагов
        sb.DrawString(font, $"Count: {enemyCount}", new Vector2(panelRect.X + 10, y), Color.White);
        y += 30;

        // Кнопки управления количеством
        UIButton decreaseBtn = new UIButton(new Rectangle(panelRect.X + 10, y, 60, 30), "-", () => {
            if (enemyCount > 1) enemyCount--;
        });
        UIButton increaseBtn = new UIButton(new Rectangle(panelRect.X + 80, y, 60, 30), "+", () => {
            enemyCount++;
        });
        
        decreaseBtn.Draw(sb, font, pixel);
        increaseBtn.Draw(sb, font, pixel);
        
        y += 40;

        // Кнопка добавления
        UIButton addBtn = new UIButton(new Rectangle(panelRect.X + 10, y, 150, 35), "Add Enemy", () => {
            if (selectedEnemyTypeId != null && selectedSpawnPointId != null)
            {
                onEnemyAdded?.Invoke(selectedEnemyTypeId, selectedSpawnPointId, enemyCount);
                Close();
            }
        });
        
        UIButton cancelBtn = new UIButton(new Rectangle(panelRect.X + 170, y, 100, 35), "Cancel", Close);
        
        addBtn.Draw(sb, font, pixel);
        cancelBtn.Draw(sb, font, pixel);

        // Подсказка
        if (selectedEnemyTypeId == null || selectedSpawnPointId == null)
        {
            sb.DrawString(font, "Select enemy and spawn point", 
                new Vector2(panelRect.X + 10, panelRect.Y + panelRect.Height - 25), Color.Yellow);
        }
    }

    public void HandleClick(Point mousePosition,
                           IReadOnlyList<string> spawnPointIds)
    {
        if (!IsOpen) return;
        if (!panelRect.Contains(mousePosition)) return;

        int y = panelRect.Y + 50 + 25;


        EnemyRegistry enemyRegistry = EnemyRegistry.Instance;
        Dictionary<string, TypeSpecification> enemies = enemyRegistry.enemies;
        // Проверка кликов по типам врагов
        foreach (var enemy in enemies.Keys)
        {
            Rectangle enemyRect = new Rectangle(panelRect.X + 10, y, panelRect.Width - 20, 35);
            if (enemyRect.Contains(mousePosition))
            {
                selectedEnemyTypeId = enemy;
                return;
            }
            y += 40;
        }

        y += 10 + 25;

        // Проверка кликов по точкам спавна
        foreach (var spawnId in spawnPointIds)
        {
            Rectangle spawnRect = new Rectangle(panelRect.X + 10, y, panelRect.Width - 20, 30);
            if (spawnRect.Contains(mousePosition))
            {
                selectedSpawnPointId = spawnId;
                return;
            }
            y += 35;
        }

        y += 10 + 30;

        // Проверка кнопок управления количеством
        Rectangle decreaseRect = new Rectangle(panelRect.X + 10, y, 60, 30);
        Rectangle increaseRect = new Rectangle(panelRect.X + 80, y, 60, 30);
        
        if (decreaseRect.Contains(mousePosition))
        {
            if (enemyCount > 1) enemyCount--;
            return;
        }
        
        if (increaseRect.Contains(mousePosition))
        {
            enemyCount++;
            return;
        }

        y += 40;

        // Проверка кнопки Add
        Rectangle addRect = new Rectangle(panelRect.X + 10, y, 150, 35);
        Rectangle cancelRect = new Rectangle(panelRect.X + 170, y, 100, 35);
        
        if (addRect.Contains(mousePosition))
        {
            if (selectedEnemyTypeId != null && selectedSpawnPointId != null)
            {
                onEnemyAdded?.Invoke(selectedEnemyTypeId, selectedSpawnPointId, enemyCount);
                Close();
            }
            return;
        }
        
        if (cancelRect.Contains(mousePosition))
        {
            Close();
            return;
        }
    }
}
