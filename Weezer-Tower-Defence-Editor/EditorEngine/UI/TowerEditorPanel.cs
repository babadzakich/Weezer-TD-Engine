using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine.Towers;
using System.Collections.Generic;
using EditorEngine.Towers.Types;

namespace EditorEngine.UI;

public class TowerEditorPanel
{
    private string id = "new_tower";
    private string name = "New Tower";
    private string className;
    private string bulletClassName;
    private int cost = 100;
    private float range = 150f;
    private float fireRate = 1f;

    private readonly UITextField idField;
    private readonly UITextField nameField;
    private readonly UITextField costField;
    private readonly UITextField rangeField;
    private readonly UITextField fireRateField;
    private readonly SelectorField selectorField;

    private readonly UIButton saveButton;

    public bool IsOpen { get; private set; }

    public TowerEditorPanel()
    {
        int x = 20, y = 20, w = 220, h = 26, gap = 34;
        List<string> options = new List<string>();
        options.Add("basic");
        options.Add("machine gun");
        options.Add("sniper");
        selectorField = new SelectorField(20, 20, options, onClick: setFromDefault);
        selectorField.Show();

        x += 100;
        y += 500;
        idField       = new(new Rectangle(x, y, w, h), id);
        nameField     = new(new Rectangle(x, y += gap, w, h), name);
        costField     = new(new Rectangle(x, y += gap, w, h), cost.ToString());
        rangeField    = new(new Rectangle(x, y += gap, w, h), range.ToString());
        fireRateField = new(new Rectangle(x, y += gap, w, h), fireRate.ToString());


        

        

        saveButton = new UIButton(
            new Rectangle(x, y + gap + 10, w, h),
            "Save Tower",
            SaveTower
        );
    }
    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    private void setFromDefault(string defaultName)
    {
        ITowerConfig defaultTower = null;
        if (defaultName == "basic")
        {
            defaultTower = new Towers.Types.BasicTower();
        }
        else if (defaultName == "machine gun")
        {
            defaultTower = new Towers.Types.MachineGunTower();
        }
        else if (defaultName == "sniper")
        {
            defaultTower = new Towers.Types.SniperTower();
        }

        id = defaultTower.Id;
        name = defaultTower.Name;
        cost = defaultTower.Cost;
        range = defaultTower.Range;
        fireRate = defaultTower.FireRate;
        bulletClassName = defaultTower.BulletClassName;
        className = defaultTower.ClassName;

        idField.Text = id;
        nameField.Text = name;
        costField.Text = cost.ToString();
        rangeField.Text = range.ToString();
        fireRateField.Text = fireRate.ToString();
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        if (!IsOpen) return;

        selectorField.Update(mouse);
        idField.Update(mouse, keyboard);
        nameField.Update(mouse, keyboard);
        costField.Update(mouse, keyboard);
        rangeField.Update(mouse, keyboard);
        fireRateField.Update(mouse, keyboard);

        saveButton.Update(mouse);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        if (!IsOpen) return;
        selectorField.Draw(sb, font, pixel);

        int panelWidth = 260;
        int panelHeight = 280;
        int x = 10;
        int y = 10;

        // Фон панели
        sb.Draw(pixel, new Rectangle(x, y, panelWidth, panelHeight), Color.Black * 0.85f);

        int labelOffset = 34;

        DrawLabel(sb, font, "Id:", new Vector2(x + 4, y + 4));
        idField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Name:", new Vector2(x + 4, y + 4 + labelOffset * 1));
        nameField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Cost:", new Vector2(x + 4, y + 4 + labelOffset * 2));
        costField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Range:", new Vector2(x + 4, y + 4 + labelOffset * 3));
        rangeField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Fire Rate:", new Vector2(x + 4, y + 4 + labelOffset * 4));
        fireRateField.Draw(sb, font, pixel);

        saveButton.Draw(sb, font, pixel);
    }

    private void DrawLabel(SpriteBatch sb, SpriteFont font, string text, Vector2 pos)
    {
        sb.DrawString(font, text, pos, Color.White);
    }

    private void SaveTower()
    {
        // Читаем значения из полей
        id = idField.Text;
        name = nameField.Text;
        
        if (int.TryParse(costField.Text, out int parsedCost))
            cost = parsedCost;
        
        if (float.TryParse(rangeField.Text, out float parsedRange))
            range = parsedRange;
        
        if (float.TryParse(fireRateField.Text, out float parsedFireRate))
            fireRate = parsedFireRate;

        // Сохраняем только JSON конфиг в Content/Towers/
        // .cs файлы создавать не нужно - они уже есть в Types/
        string configDir = "Content/Towers";
        System.IO.Directory.CreateDirectory(configDir);
        
        string configPath = System.IO.Path.Combine(configDir, $"{id}.json");
        
        var config = new {
            Id = id,
            Name = name,
            Cost = cost,
            Range = range,
            FireRate = fireRate
        };
        
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        string json = System.Text.Json.JsonSerializer.Serialize(config, options);
        System.IO.File.WriteAllText(configPath, json);
        
        Console.WriteLine($"Tower config saved: {configPath}");
    }
}
