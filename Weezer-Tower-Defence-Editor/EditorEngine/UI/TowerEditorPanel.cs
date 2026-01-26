using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine.Towers;
using IOPath = System.IO.Path;

namespace EditorEngine.UI;

public class TowerEditorPanel
{
    private string id = "new_tower";
    private string name = "New Tower";
    private int cost = 100;
    private float range = 150f;
    private float fireRate = 1f;
    private float damage = 25f;

    private readonly UITextField idField;
    private readonly UITextField nameField;
    private readonly UITextField costField;
    private readonly UITextField rangeField;
    private readonly UITextField fireRateField;
    private readonly UITextField damageField;
    private readonly UITextField levelsCountField;

    private string lastLevelsCountText = "0";

    private readonly List<UpgradeLevelUI> upgradeLevels = new();
    private readonly UIButton saveButton;
    private readonly UIButton newTowerButton;

    private readonly List<UIButton> towerSelectionButtons = new();
    private readonly List<UIButton> towerDeleteButtons = new();

    public bool IsOpen { get; private set; }

    private class UpgradeLevelUI
    {
        public UITextField CostField;
        public UITextField RangeField;
        public UITextField FireRateField;
        public UITextField DamageField;

        public UpgradeLevelUI(int y, int x)
        {
            int h = 26;
            int w = 55;
            int spacing = 5;
            CostField = new UITextField(new Rectangle(x, y, w, h), "50");
            RangeField = new UITextField(new Rectangle(x + w + spacing, y, w, h), "170");
            FireRateField = new UITextField(new Rectangle(x + (w + spacing) * 2, y, w, h), "1.2");
            DamageField = new UITextField(new Rectangle(x + (w + spacing) * 3, y, w, h), "30");
        }

        public void Update(MouseState mouse, KeyboardState keyboard)
        {
            CostField.Update(mouse, keyboard);
            RangeField.Update(mouse, keyboard);
            FireRateField.Update(mouse, keyboard);
            DamageField.Update(mouse, keyboard);
        }

        public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
        {
            CostField.Draw(sb, font, pixel);
            RangeField.Draw(sb, font, pixel);
            FireRateField.Draw(sb, font, pixel);
            DamageField.Draw(sb, font, pixel);
        }
    }

    public TowerEditorPanel()
    {
        int fieldX = 150; // Увеличено со 110 до 150
        int fieldW = 140;
        int h = 26;
        int gap = 30;
        int currentY = 20;

        idField          = new(new Rectangle(fieldX, currentY, fieldW, h), id);
        nameField        = new(new Rectangle(fieldX, currentY += gap, fieldW, h), name);
        costField        = new(new Rectangle(fieldX, currentY += gap, fieldW, h), cost.ToString());
        rangeField       = new(new Rectangle(fieldX, currentY += gap, fieldW, h), range.ToString());
        fireRateField    = new(new Rectangle(fieldX, currentY += gap, fieldW, h), fireRate.ToString());
        damageField      = new(new Rectangle(fieldX, currentY += gap, fieldW, h), damage.ToString());
        levelsCountField = new(new Rectangle(fieldX, currentY += gap, fieldW, h), "0");

        newTowerButton = new UIButton(
            new Rectangle(20, currentY += gap + 10, 125, h),
            "New Tower",
            ClearFields
        );

        saveButton = new UIButton(
            new Rectangle(155, currentY, 125, h),
            "Save Tower",
            SaveTower
        );
    }

    private void ClearFields()
    {
        idField.Text = "new_tower";
        nameField.Text = "New Tower";
        costField.Text = "100";
        rangeField.Text = "150";
        fireRateField.Text = "1";
        damageField.Text = "25";
        levelsCountField.Text = "0";
        upgradeLevels.Clear();
        lastLevelsCountText = "0";
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        if (IsOpen)
        {
            RefreshTowerList();
        }
    }

    private void RefreshTowerList()
    {
        towerSelectionButtons.Clear();
        towerDeleteButtons.Clear();

        string configDir = "Content/Towers";
        if (!Directory.Exists(configDir)) return;

        int startX = 320;
        int currentY = 50;
        int buttonW = 180;
        int deleteW = 50;
        int h = 26;
        int spacing = 5;

        foreach (var file in Directory.GetFiles(configDir, "*.json"))
        {
            string towerId = IOPath.GetFileNameWithoutExtension(file);
            
            var selectBtn = new UIButton(
                new Rectangle(startX, currentY, buttonW, h),
                towerId,
                () => LoadTower(towerId)
            );
            towerSelectionButtons.Add(selectBtn);

            var deleteBtn = new UIButton(
                new Rectangle(startX + buttonW + spacing, currentY, deleteW, h),
                "Excl",
                () => DeleteTower(towerId)
            );
            towerDeleteButtons.Add(deleteBtn);

            currentY += h + spacing;
        }
    }

    private void LoadTower(string towerId)
    {
        string configPath = IOPath.Combine("Content/Towers", $"{towerId}.json");
        if (!File.Exists(configPath)) return;

        try
        {
            string json = File.ReadAllText(configPath);
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = System.Text.Json.JsonSerializer.Deserialize<TowerData>(json, options);

            if (data != null)
            {
                idField.Text = data.Id;
                nameField.Text = data.Name;
                costField.Text = data.Cost.ToString();
                rangeField.Text = data.Range.ToString();
                fireRateField.Text = data.FireRate.ToString();
                damageField.Text = data.Damage.ToString();
                
                int upCount = data.UpgradeLevels?.Count ?? 0;
                levelsCountField.Text = upCount.ToString();
                lastLevelsCountText = upCount.ToString();
                
                upgradeLevels.Clear();
                if (data.UpgradeLevels != null)
                {
                    for (int i = 0; i < data.UpgradeLevels.Count; i++)
                    {
                        var up = data.UpgradeLevels[i];
                        int y = 300 + i * 30;
                        var upUI = new UpgradeLevelUI(y, 20);
                        upUI.CostField.Text = up.UpgradeCost.ToString();
                        upUI.RangeField.Text = up.Range.ToString();
                        upUI.FireRateField.Text = up.FireRate.ToString();
                        upUI.DamageField.Text = up.Damage.ToString();
                        upgradeLevels.Add(upUI);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading tower {towerId}: {ex.Message}");
        }
    }

    private class TowerData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Cost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public float Damage { get; set; }
        public List<UpgradeData> UpgradeLevels { get; set; }
    }

    private class UpgradeData
    {
        public int UpgradeCost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public float Damage { get; set; }
    }

    private void DeleteTower(string towerId)
    {
        string configPath = IOPath.Combine("Content/Towers", $"{towerId}.json");
        if (File.Exists(configPath))
        {
            File.Delete(configPath);
            RefreshTowerList();
            if (idField.Text == towerId)
            {
                ClearFields();
            }
        }
    }

    public void Update(MouseState mouse, KeyboardState keyboard, MouseState previousMouse)
    {
        if (!IsOpen) return;

        idField.Update(mouse, keyboard);
        nameField.Update(mouse, keyboard);
        costField.Update(mouse, keyboard);
        rangeField.Update(mouse, keyboard);
        fireRateField.Update(mouse, keyboard);
        damageField.Update(mouse, keyboard);
        levelsCountField.Update(mouse, keyboard);

        // Проверка изменения количества уровней
        if (levelsCountField.Text != lastLevelsCountText)
        {
            if (int.TryParse(levelsCountField.Text, out int count))
            {
                if (count < 0) count = 0;
                if (count > 10) count = 10; // Ограничение

                while (upgradeLevels.Count < count)
                {
                    int y = 300 + upgradeLevels.Count * 30;
                    upgradeLevels.Add(new UpgradeLevelUI(y, 20));
                }
                while (upgradeLevels.Count > count)
                {
                    upgradeLevels.RemoveAt(upgradeLevels.Count - 1);
                }
                lastLevelsCountText = count.ToString();
            }
        }

        foreach (var level in upgradeLevels)
        {
            level.Update(mouse, keyboard);
        }

        newTowerButton.Update(mouse);

        // Кнопка сохранения теперь динамически перемещается вниз
        int saveY = Math.Max(260, 300 + upgradeLevels.Count * 30 + 10);
        saveButton.Bounds.Y = saveY;
        saveButton.Update(mouse);

        foreach (var btn in towerSelectionButtons.ToList()) btn.Update(mouse);
        foreach (var btn in towerDeleteButtons.ToList()) btn.Update(mouse);
    }

    public Rectangle GetBounds()
    {
        // Панель стала шире для списка башен
        int height = Math.Max(350, 300 + upgradeLevels.Count * 30 + 60);
        return new Rectangle(10, 10, 560, height);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        if (!IsOpen) return;

        int x = 10;
        var bounds = GetBounds();

        // Фон панели
        sb.Draw(pixel, bounds, Color.Black * 0.85f);

        // Рисуем лейблы
        DrawLabel(sb, font, "Id:", new Vector2(x + 10, idField.Bounds.Y + 2));
        idField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Name:", new Vector2(x + 10, nameField.Bounds.Y + 2));
        nameField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Base Cost (1):", new Vector2(x + 10, costField.Bounds.Y + 2));
        costField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Base Range:", new Vector2(x + 10, rangeField.Bounds.Y + 2));
        rangeField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Base F.Rate:", new Vector2(x + 10, fireRateField.Bounds.Y + 2));
        fireRateField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Base Damage:", new Vector2(x + 10, damageField.Bounds.Y + 2));
        damageField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Upgr. Levels:", new Vector2(x + 10, levelsCountField.Bounds.Y + 2));
        levelsCountField.Draw(sb, font, pixel);

        newTowerButton.Draw(sb, font, pixel);

        // Секция улучшений
        if (upgradeLevels.Count > 0)
        {
            int labelY = 275;
            DrawLabel(sb, font, "Lv: Cost, Range, Speed, Damage", new Vector2(x + 10, labelY));
            
            for (int i = 0; i < upgradeLevels.Count; i++)
            {
                var level = upgradeLevels[i];
                DrawLabel(sb, font, $"{i + 2}:", new Vector2(x + 5, level.CostField.Bounds.Y + 2));
                level.Draw(sb, font, pixel);
            }
        }

        // Кнопка сохранения в самом низу
        saveButton.Draw(sb, font, pixel);

        // Список башен справа
        DrawLabel(sb, font, "Packed Towers:", new Vector2(320, 20));
        foreach (var btn in towerSelectionButtons) btn.Draw(sb, font, pixel);
        foreach (var btn in towerDeleteButtons) btn.Draw(sb, font, pixel);
    }

    private void DrawLabel(SpriteBatch sb, SpriteFont font, string text, Vector2 pos)
    {
        sb.DrawString(font, text, pos, Color.White);
    }

    private void SaveTower()
    {
        id = idField.Text;
        name = nameField.Text;
        
        if (string.IsNullOrWhiteSpace(id)) return;

        int.TryParse(costField.Text, out cost);
        float.TryParse(rangeField.Text, out range);
        float.TryParse(fireRateField.Text, out fireRate);
        float.TryParse(damageField.Text, out damage);

        var upgradeLevelsData = new List<object>();
        foreach (var level in upgradeLevels)
        {
            int uCost = 0;
            float uRange = 0;
            float uFireRate = 0;
            float uDamage = 0;

            int.TryParse(level.CostField.Text, out uCost);
            float.TryParse(level.RangeField.Text, out uRange);
            float.TryParse(level.FireRateField.Text, out uFireRate);
            float.TryParse(level.DamageField.Text, out uDamage);

            upgradeLevelsData.Add(new {
                UpgradeCost = uCost,
                Range = uRange,
                FireRate = uFireRate,
                Damage = uDamage
            });
        }

        string configDir = "Content/Towers";
        Directory.CreateDirectory(configDir);
        string configPath = IOPath.Combine(configDir, $"{id}.json");
        
        var config = new {
            Id = id,
            Name = name,
            Cost = cost,
            Range = range,
            FireRate = fireRate,
            Damage = damage,
            UpgradeLevels = upgradeLevelsData
        };
        
        var options = new System.Text.Json.JsonSerializerOptions { 
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };
        string json = System.Text.Json.JsonSerializer.Serialize(config, options);
        File.WriteAllText(configPath, json);
        
        Console.WriteLine($"Tower config saved: {configPath}");
    }
}
