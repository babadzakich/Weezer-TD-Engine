using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine.Towers;

namespace EditorEngine.UI;

public class TowerEditorPanel
{
    private string id = "new_tower";
    private string name = "New Tower";
    private int cost = 100;
    private float range = 150f;
    private float fireRate = 1f;
    private int levels = 1; // includes base level (level 0)

    private readonly UITextField idField;
    private readonly UITextField nameField;
    private readonly UITextField costField;
    private readonly UITextField rangeField;
    private readonly UITextField fireRateField;
    private readonly UITextField levelsField;

    private readonly UIButton saveButton;

    public bool IsOpen { get; private set; }

    public Rectangle GetBounds()
    {
        int panelX = 10;
        int panelY = 10;
        int panelWidth = 270;
        int panelHeight = 50 + (6 * 34) + (_upgradeFields.Count > 0 ? 30 + (_upgradeFields.Count * 34 * 3) : 0) + 50;
        return new Rectangle(panelX, panelY, panelWidth, panelHeight);
    }

    private class UpgradeLevelFields
    {
        public UITextField UpgradeCostField;
        public UITextField RangeField;
        public UITextField FireRateField;
    }

    private readonly System.Collections.Generic.List<UpgradeLevelFields> _upgradeFields = new();

    public TowerEditorPanel()
    {
        int labelX = 14;
        int fieldX = 110; // Увеличено с 100 до 110
        int y = 40; // Начальная позиция Y (увеличено для заголовка)
        int w = 160; // Ширина полей ввода (уменьшено для вмещения в панель 270)
        int h = 26;
        int gap = 34;

        idField       = new(new Rectangle(fieldX, y, w, h), id);
        nameField     = new(new Rectangle(fieldX, y += gap, w, h), name);
        costField     = new(new Rectangle(fieldX, y += gap, w, h), cost.ToString());
        rangeField    = new(new Rectangle(fieldX, y += gap, w, h), range.ToString());
        fireRateField = new(new Rectangle(fieldX, y += gap, w, h), fireRate.ToString());
        levelsField   = new(new Rectangle(fieldX, y += gap, w, h), levels.ToString());

        // start fields after base tower section
        RebuildUpgradeFields(fieldX, y + gap + 10, w, h, gap);

        int buttonY = y + gap + 20 + (_upgradeFields.Count * gap * 3);
        saveButton = new UIButton(
            new Rectangle(fieldX, buttonY, w, h),
            "Save Tower",
            SaveTower
        );
    }
    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public void Update(MouseState mouse, KeyboardState keyboard, MouseState previousMouse)
    {
        if (!IsOpen) return;

        // #region agent log
        System.IO.File.AppendAllText(@"debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "F", location = "TowerEditorPanel.cs:69", message = "Update called", data = new { isOpen = IsOpen, mouseX = mouse.X, mouseY = mouse.Y, mouseLeft = mouse.LeftButton.ToString(), prevMouseLeft = previousMouse.LeftButton.ToString() }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
        // #endregion

        idField.Update(mouse, keyboard);
        nameField.Update(mouse, keyboard);
        costField.Update(mouse, keyboard);
        rangeField.Update(mouse, keyboard);
        fireRateField.Update(mouse, keyboard);
        levelsField.Update(mouse, keyboard);

        if (int.TryParse(levelsField.Text, out int parsedLevels))
        {
            parsedLevels = System.Math.Clamp(parsedLevels, 1, 10);
            if (parsedLevels != levels)
            {
                levels = parsedLevels;
                int x = idField.Bounds.X;
                int yAfterLevels = levelsField.Bounds.Y + levelsField.Bounds.Height + 8;
                RebuildUpgradeFields(x, yAfterLevels, idField.Bounds.Width, idField.Bounds.Height, 34);
                int buttonY = yAfterLevels + (_upgradeFields.Count * 34 * 3) + 20;
                saveButton.Bounds = new Rectangle(x, buttonY, idField.Bounds.Width, idField.Bounds.Height);
            }
        }

        foreach (var lvl in _upgradeFields)
        {
            lvl.UpgradeCostField.Update(mouse, keyboard);
            lvl.RangeField.Update(mouse, keyboard);
            lvl.FireRateField.Update(mouse, keyboard);
        }

        // Обработка клика кнопки с проверкой перехода из Released в Pressed
        bool containsMouse = saveButton.Bounds.Contains(mouse.Position);
        bool isPressed = mouse.LeftButton == ButtonState.Pressed;
        bool wasReleased = previousMouse.LeftButton == ButtonState.Released;
        
        // #region agent log
        System.IO.File.AppendAllText(@"debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "A", location = "TowerEditorPanel.cs:101", message = "Button click check", data = new { containsMouse, isPressed, wasReleased, buttonBounds = new { x = saveButton.Bounds.X, y = saveButton.Bounds.Y, w = saveButton.Bounds.Width, h = saveButton.Bounds.Height }, mousePos = new { x = mouse.X, y = mouse.Y }, onClickNull = saveButton.OnClick == null }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
        // #endregion
        
        if (containsMouse && isPressed && wasReleased)
        {
            // #region agent log
            System.IO.File.AppendAllText(@"debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "TowerEditorPanel.cs:108", message = "Button click triggered", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
            // #endregion
            saveButton.OnClick?.Invoke();
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        if (!IsOpen) return;

        int labelX = 14;
        int panelX = 10;
        int panelY = 10;
        int panelWidth = 270;
        int panelHeight = 50 + (6 * 34) + (_upgradeFields.Count > 0 ? 30 + (_upgradeFields.Count * 34 * 3) : 0) + 50;
        
        // Фон панели
        sb.Draw(pixel, new Rectangle(panelX, panelY, panelWidth, panelHeight), Color.Black * 0.85f);

        // Заголовок
        sb.DrawString(font, "Tower Editor", new Vector2(panelX + 10, panelY + 5), Color.Yellow);

        int baseY = panelY + 30;
        int labelOffset = 34;

        // Базовые поля
        DrawLabel(sb, font, "Id:", new Vector2(labelX, idField.Bounds.Y + 4));
        idField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Name:", new Vector2(labelX, nameField.Bounds.Y + 4));
        nameField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Cost:", new Vector2(labelX, costField.Bounds.Y + 4));
        costField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Range:", new Vector2(labelX, rangeField.Bounds.Y + 4));
        rangeField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "FireRate:", new Vector2(labelX, fireRateField.Bounds.Y + 4));
        fireRateField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Levels:", new Vector2(labelX, levelsField.Bounds.Y + 4));
        levelsField.Draw(sb, font, pixel);

        // Поля апгрейдов
        int uBaseY = levelsField.Bounds.Y + levelsField.Bounds.Height + 10;
        if (_upgradeFields.Count > 0)
        {
            sb.DrawString(font, "Upgrade Levels:", new Vector2(labelX, uBaseY), Color.LightGreen);
        }

        for (int i = 0; i < _upgradeFields.Count; i++)
        {
            var lvl = _upgradeFields[i];
            
            DrawLabel(sb, font, $"L{i + 1} Cost:", new Vector2(labelX, lvl.UpgradeCostField.Bounds.Y + 4));
            lvl.UpgradeCostField.Draw(sb, font, pixel);

            DrawLabel(sb, font, $"L{i + 1} Range:", new Vector2(labelX, lvl.RangeField.Bounds.Y + 4));
            lvl.RangeField.Draw(sb, font, pixel);

            DrawLabel(sb, font, $"L{i + 1} FireRate:", new Vector2(labelX, lvl.FireRateField.Bounds.Y + 4));
            lvl.FireRateField.Draw(sb, font, pixel);
        }

        saveButton.Draw(sb, font, pixel);
    }

    private void DrawLabel(SpriteBatch sb, SpriteFont font, string text, Vector2 pos)
    {
        sb.DrawString(font, text, pos, Color.White);
    }

    private void SaveTower()
    {
        // #region agent log
        System.IO.File.AppendAllText(@"debug.log", System.Text.Json.JsonSerializer.Serialize(new { sessionId = "debug-session", runId = "run1", hypothesisId = "C", location = "TowerEditorPanel.cs:179", message = "SaveTower called", data = new { }, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }) + "\n");
        // #endregion
        
        // Читаем значения из полей
        id = idField.Text;
        name = nameField.Text;
        
        if (int.TryParse(costField.Text, out int parsedCost))
            cost = parsedCost;
        
        if (float.TryParse(rangeField.Text, out float parsedRange))
            range = parsedRange;
        
        if (float.TryParse(fireRateField.Text, out float parsedFireRate))
            fireRate = parsedFireRate;

        if (int.TryParse(levelsField.Text, out int parsedLevels))
            levels = System.Math.Clamp(parsedLevels, 1, 10);

        // Сохраняем только JSON конфиг в Content/Towers/
        // .cs файлы создавать не нужно - они уже есть в Types/
        string configDir = "Content/Towers";
        System.IO.Directory.CreateDirectory(configDir);
        
        string configPath = System.IO.Path.Combine(configDir, $"{id}.json");
        
        var upgradeLevels = new System.Collections.Generic.List<object>();
        foreach (var lvl in _upgradeFields)
        {
            int.TryParse(lvl.UpgradeCostField.Text, out int uCost);
            float.TryParse(lvl.RangeField.Text, out float uRange);
            float.TryParse(lvl.FireRateField.Text, out float uFireRate);
            upgradeLevels.Add(new
            {
                UpgradeCost = uCost,
                Range = uRange,
                FireRate = uFireRate
            });
        }

        var config = new
        {
            Id = id,
            Name = name,
            Cost = cost,
            Range = range,
            FireRate = fireRate,
            UpgradeLevels = upgradeLevels
        };
        
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        string json = System.Text.Json.JsonSerializer.Serialize(config, options);
        System.IO.File.WriteAllText(configPath, json);
        
        Console.WriteLine($"Tower config saved: {configPath}");
    }

    private void RebuildUpgradeFields(int x, int startY, int w, int h, int gap)
    {
        _upgradeFields.Clear();

        int upgradeCount = System.Math.Max(0, levels - 1);

        int y = startY;
        for (int i = 0; i < upgradeCount; i++)
        {
            var costF = new UITextField(new Rectangle(x, y, w, h), "50");
            var rangeF = new UITextField(new Rectangle(x, y + gap, w, h), range.ToString());
            var frF = new UITextField(new Rectangle(x, y + gap * 2, w, h), fireRate.ToString());
            y += gap * 3; // Переход к следующему уровню

            _upgradeFields.Add(new UpgradeLevelFields
            {
                UpgradeCostField = costF,
                RangeField = rangeF,
                FireRateField = frF
            });
        }
    }
}
