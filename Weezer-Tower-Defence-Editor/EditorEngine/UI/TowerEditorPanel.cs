using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine.Towers;
using IOPath = System.IO.Path;
using EditorEngine.Towers.Types;
using SimulationEngine.TowerRelated;

namespace EditorEngine.UI;

public class TowerEditorPanel
{
    private string id = "new_tower";
    private string name = "New Tower";
    private int cost = 100;
    private float range = 150f;
    private float fireRate = 1f;
    private float damage = 25f;

    private ITowerConfig towerConfig;

    private readonly int top = 20;
    private readonly int left = 20;
    private readonly int width = 1500;
    private readonly int height = 600;
    private readonly int leftInputPadding = 300;
    private readonly int upperInputPadding = 50;
    private readonly int inputHeight = 40;
    private readonly int dropListHeight = 200;

    private readonly UITextField idField;
    private readonly UITextField nameField;
    private readonly UITextField classField;
    private readonly UITextField bulletClassField;
    private readonly UITextField costField;
    private readonly UITextField rangeField;
    private readonly UITextField fireRateField;
    private readonly UITextField damageField;
    private readonly UITextField levelsCountField;
    private readonly SelectorField selectorField;

    private string lastLevelsCountText = "0";

    private readonly List<UpgradeLevelUI> upgradeLevels = new();
    private readonly UIButton saveButton;
    private readonly UIButton newTowerButton;

    private readonly List<UIButton> towerSelectionButtons = new();
    private readonly List<UIButton> towerDeleteButtons = new();

    public bool IsOpen { get; private set; }

    public Rectangle GetBounds() => new Rectangle(left, top, width, height + 300);

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
        List<string> options = new() { "basic", "machine gun", "sniper" };
        towerConfig = new EditorEngine.Towers.Types.BasicTower();

        selectorField = new SelectorField(top, left, options, onClick: setFromDefault);
        selectorField.Show();

        int x = left + leftInputPadding;
        int y = top + dropListHeight;
        int w = 400;
        int h = 26;

        idField          = new(new Rectangle(x, y, w, h), towerConfig.Id, updateTextField, "id");
        nameField        = new(new Rectangle(x, y += inputHeight, w, h), towerConfig.Name, updateTextField, "name");
        classField       = new(new Rectangle(x, y += inputHeight, w, h), towerConfig.ClassName, updateTextField, "className");
        bulletClassField = new(new Rectangle(x, y += inputHeight, w, h), towerConfig.BulletClassName, updateTextField, "bulletClassName");
        costField        = new(new Rectangle(x, y += inputHeight, w, h), towerConfig.Cost.ToString(), updateTextField, "cost");
        rangeField       = new(new Rectangle(x, y += inputHeight, w, h), towerConfig.Range.ToString(), updateTextField, "range");
        fireRateField    = new(new Rectangle(x, y += inputHeight, w, h), towerConfig.FireRate.ToString(), updateTextField, "fireRate");
        damageField      = new(new Rectangle(x, y += inputHeight, w, h), damage.ToString(), updateTextField, "damage");
        levelsCountField = new(new Rectangle(x, y += inputHeight, w, h), "0", updateTextField, "levelsCount");

        newTowerButton = new UIButton(
            new Rectangle(left, y + inputHeight + 10, w, 50),
            "New Tower",
            ClearFields
        );

        saveButton = new UIButton(
            new Rectangle(left, y + (inputHeight + 10) * 2, w, 50),
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
            selectorField.Show();
        }
        else
        {
            selectorField.Hide();
        }
    }

    private void updateTextField(string text, string fieldId) {
        switch (fieldId)
        {
            case "id":
                towerConfig.Id = text;
                break;
            case "name":
                towerConfig.Name = text;
                break;
            case "className":
                towerConfig.ClassName = text;
                break;
            case "bulletClassName":
                towerConfig.BulletClassName = text;
                break;
            case "cost":
                if (int.TryParse(text, out int parsedCost)) towerConfig.Cost = parsedCost;
                break;
            case "range":
                if (float.TryParse(text, out float parsedRange)) towerConfig.Range = parsedRange;
                break;
            case "fireRate":
                if (float.TryParse(text, out float parsedFireRate)) towerConfig.FireRate = parsedFireRate;
                break;
            case "damage":
                if (float.TryParse(text, out float parsedDamage)) damage = parsedDamage;
                break;
        }
    }

    private void setFromDefault(string defaultName)
    {
        if (defaultName == "basic") towerConfig = new EditorEngine.Towers.Types.BasicTower();
        else if (defaultName == "machine gun") towerConfig = new EditorEngine.Towers.Types.MachineGunTower();
        else if (defaultName == "sniper") towerConfig = new EditorEngine.Towers.Types.SniperTower();

        idField.Text = towerConfig.Id;
        nameField.Text = towerConfig.Name;
        classField.Text = towerConfig.ClassName;
        bulletClassField.Text = towerConfig.BulletClassName;
        costField.Text = towerConfig.Cost.ToString();
        rangeField.Text = towerConfig.Range.ToString();
        fireRateField.Text = towerConfig.FireRate.ToString();
    }

    private void RefreshTowerList()
    {
        towerSelectionButtons.Clear();
        towerDeleteButtons.Clear();
        string configDir = "Content/Towers";
        if (!Directory.Exists(configDir)) return;

        int startX = 320, startY = 50, btnW = 150, btnH = 25, delW = 30;
        foreach (var file in Directory.GetFiles(configDir, "*.json"))
        {
            string towerId = IOPath.GetFileNameWithoutExtension(file);
            towerSelectionButtons.Add(new UIButton(new Rectangle(startX, startY, btnW, btnH), towerId, () => LoadTower(towerId)));
            towerDeleteButtons.Add(new UIButton(new Rectangle(startX + btnW + 5, startY, delW, btnH), "X", () => DeleteTower(towerId)));
            startY += btnH + 5;
        }
    }

    private void LoadTower(string towerId)
    {
        try {
            string json = File.ReadAllText(IOPath.Combine("Content/Towers", $"{towerId}.json"));
            var data = System.Text.Json.JsonSerializer.Deserialize<TowerSaveData>(json);
            if (data == null) return;
            idField.Text = data.Id;
            nameField.Text = data.Name;
            costField.Text = data.Cost.ToString();
            rangeField.Text = data.Range.ToString();
            fireRateField.Text = data.FireRate.ToString();
            damageField.Text = (data.Damage > 0 ? data.Damage : 25).ToString();
            levelsCountField.Text = (data.Upgrades?.Count ?? 0).ToString();
            upgradeLevels.Clear();
            if (data.Upgrades != null) {
                int startY = levelsCountField.Bounds.Y + 40;
                foreach (var u in data.Upgrades) {
                    var ui = new UpgradeLevelUI(startY, 110);
                    ui.CostField.Text = u.Cost.ToString();
                    ui.RangeField.Text = u.Range.ToString();
                    ui.FireRateField.Text = u.FireRate.ToString();
                    ui.DamageField.Text = u.Damage.ToString();
                    upgradeLevels.Add(ui);
                    startY += 35;
                }
            }
            lastLevelsCountText = levelsCountField.Text;
        } catch (Exception ex) { Console.WriteLine("Error loading tower: " + ex.Message); }
    }

    private void DeleteTower(string towerId)
    {
        try {
            File.Delete(IOPath.Combine("Content/Towers", $"{towerId}.json"));
            RefreshTowerList();
        } catch (Exception ex) { Console.WriteLine("Error deleting tower: " + ex.Message); }
    }

    public void Update(MouseState mouse, KeyboardState keyboard, MouseState previousMouse)
    {
        if (!IsOpen) return;
        selectorField.Update(mouse);
        idField.Update(mouse, keyboard);
        nameField.Update(mouse, keyboard);
        classField.Update(mouse, keyboard);
        bulletClassField.Update(mouse, keyboard);
        costField.Update(mouse, keyboard);
        rangeField.Update(mouse, keyboard);
        fireRateField.Update(mouse, keyboard);
        damageField.Update(mouse, keyboard);
        levelsCountField.Update(mouse, keyboard);

        if (levelsCountField.Text != lastLevelsCountText) {
            if (int.TryParse(levelsCountField.Text, out int count)) {
                count = Math.Clamp(count, 0, 5);
                while (upgradeLevels.Count < count) upgradeLevels.Add(new UpgradeLevelUI(levelsCountField.Bounds.Y + 40 + upgradeLevels.Count * 35, 110));
                while (upgradeLevels.Count > count) upgradeLevels.RemoveAt(upgradeLevels.Count - 1);
                lastLevelsCountText = count.ToString();
            }
        }

        foreach (var u in upgradeLevels) u.Update(mouse, keyboard);
        newTowerButton.Update(mouse);
        saveButton.Update(mouse);
        foreach (var btn in towerSelectionButtons) btn.Update(mouse);
        foreach (var btn in towerDeleteButtons) btn.Update(mouse);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        if (!IsOpen) return;
        int panelWidth = width;
        int panelHeight = height + 300;
        sb.Draw(pixel, new Rectangle(left, top, panelWidth, panelHeight), Color.Black * 0.85f);
        selectorField.Draw(sb, font, pixel);

        int x = left + leftInputPadding;
        DrawLabel(sb, font, "Id:", new Vector2(idField.Bounds.X - 120, idField.Bounds.Y + 2));
        idField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Name:", new Vector2(nameField.Bounds.X - 120, nameField.Bounds.Y + 2));
        nameField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Class:", new Vector2(classField.Bounds.X - 120, classField.Bounds.Y + 2));
        classField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Bullet:", new Vector2(bulletClassField.Bounds.X - 120, bulletClassField.Bounds.Y + 2));
        bulletClassField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Cost:", new Vector2(costField.Bounds.X - 120, costField.Bounds.Y + 2));
        costField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Range:", new Vector2(rangeField.Bounds.X - 120, rangeField.Bounds.Y + 2));
        rangeField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "F.Rate:", new Vector2(fireRateField.Bounds.X - 120, fireRateField.Bounds.Y + 2));
        fireRateField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Damage:", new Vector2(damageField.Bounds.X - 120, damageField.Bounds.Y + 2));
        damageField.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Levels:", new Vector2(levelsCountField.Bounds.X - 120, levelsCountField.Bounds.Y + 2));
        levelsCountField.Draw(sb, font, pixel);

        for (int i = 0; i < upgradeLevels.Count; i++) {
            DrawLabel(sb, font, $"Lvl {i + 2}:", new Vector2(upgradeLevels[i].CostField.Bounds.X - 60, upgradeLevels[i].CostField.Bounds.Y + 2));
            upgradeLevels[i].Draw(sb, font, pixel);
        }

        newTowerButton.Draw(sb, font, pixel);
        saveButton.Draw(sb, font, pixel);
        DrawLabel(sb, font, "Packed Towers:", new Vector2(320, 20));
        foreach (var btn in towerSelectionButtons) btn.Draw(sb, font, pixel);
        foreach (var btn in towerDeleteButtons) btn.Draw(sb, font, pixel);
    }

    private void DrawLabel(SpriteBatch sb, SpriteFont font, string text, Vector2 pos) => sb.DrawString(font, text, pos, Color.White);

    private void SaveTower()
    {
        string towerId = idField.Text;
        if (string.IsNullOrWhiteSpace(towerId)) return;
        var upgrades = upgradeLevels.Select(u => new TowerUpgradeData {
            Cost = int.TryParse(u.CostField.Text, out int c) ? c : 0,
            Range = float.TryParse(u.RangeField.Text, out float r) ? r : 0,
            FireRate = float.TryParse(u.FireRateField.Text, out float f) ? f : 0,
            Damage = float.TryParse(u.DamageField.Text, out float d) ? d : 0
        }).ToList();

        var config = new TowerSaveData {
            Id = towerId,
            Name = nameField.Text,
            ClassName = classField.Text,
            BulletClassName = bulletClassField.Text,
            Cost = int.TryParse(costField.Text, out int co) ? co : 0,
            Range = float.TryParse(rangeField.Text, out float ra) ? ra : 0,
            FireRate = float.TryParse(fireRateField.Text, out float fr) ? fr : 0,
            Damage = float.TryParse(damageField.Text, out float da) ? da : 0,
            Upgrades = upgrades
        };

        string configDir = "Content/Towers";
        Directory.CreateDirectory(configDir);
        File.WriteAllText(IOPath.Combine(configDir, $"{towerId}.json"), System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        RefreshTowerList();
    }

    private class TowerSaveData {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClassName { get; set; }
        public string BulletClassName { get; set; }
        public int Cost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public float Damage { get; set; }
        public List<TowerUpgradeData> Upgrades { get; set; }
    }

    private class TowerUpgradeData {
        public int Cost { get; set; }
        public float Range { get; set; }
        public float FireRate { get; set; }
        public float Damage { get; set; }
    }
}
