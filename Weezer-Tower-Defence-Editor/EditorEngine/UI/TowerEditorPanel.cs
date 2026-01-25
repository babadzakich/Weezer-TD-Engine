using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine.Towers;
using System.Collections.Generic;
using EditorEngine.Towers.Types;
using SimulationEngine.TowerRelated;

namespace EditorEngine.UI;

public class TowerEditorPanel
{
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
    private readonly SelectorField selectorField;

    private readonly UIButton saveButton;

    public bool IsOpen { get; private set; }

    public TowerEditorPanel()
    {
        List<string> options = new() { "basic", "machine gun", "sniper" };
        towerConfig = new Towers.Types.BasicTower();

        selectorField = new SelectorField(top, left, options, onClick: setFromDefault);
        selectorField.Show();

        
        int x = left + leftInputPadding;
        int y = top + dropListHeight;
        int w = 1000, h = 26;
        idField       = new(new Rectangle(x, y, w, h), updateTextField, "id", towerConfig.Id);
        nameField     = new(new Rectangle(x, y += inputHeight, w, h), updateTextField, "name", towerConfig.Name);
        classField     = new(new Rectangle(x, y += inputHeight, w, h), updateTextField, "className", towerConfig.ClassName);
        bulletClassField = new(new Rectangle(x, y += inputHeight, w, h), updateTextField, "bulletClassName", towerConfig.BulletClassName);
        costField     = new(new Rectangle(x, y += inputHeight, w, h), updateTextField, "cost", towerConfig.Cost.ToString());
        rangeField    = new(new Rectangle(x, y += inputHeight, w, h), updateTextField, "range", towerConfig.Range.ToString());
        fireRateField = new(new Rectangle(x, y += inputHeight, w, h),updateTextField, "fireRate", towerConfig.FireRate.ToString());


        saveButton = new UIButton(
            new Rectangle(left, y + inputHeight + 10, w, 50),
            "Save Tower",
            SaveTower
        );
    }
    public void Toggle()
    {
        IsOpen = !IsOpen;
        if (IsOpen)
        {
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
                if (int.TryParse(text, out int parsedCost))
                {
                    towerConfig.Cost = parsedCost;
                } else
                {
                    costField.Text = towerConfig.Cost.ToString();
                }
                break;

            case "range":
                if (float.TryParse(text, out float parsedRange))
                {
                    towerConfig.Range = parsedRange;
                } else
                {
                    rangeField.Text = towerConfig.Range.ToString();
                }
                break;

            case "fireRate":
                if (float.TryParse(text, out float parsedFireRate))
                {
                    towerConfig.FireRate = parsedFireRate;
                } else
                {
                    fireRateField.Text = towerConfig.FireRate.ToString();
                }
                break;

            default:
                break;
        }
    }

    private void setFromDefault(string defaultName)
    {
        if (defaultName == "basic")
        {
            towerConfig = new Towers.Types.BasicTower();
        }
        else if (defaultName == "machine gun")
        {
            towerConfig = new Towers.Types.MachineGunTower();
        }
        else if (defaultName == "sniper")
        {
            towerConfig = new Towers.Types.SniperTower();
        }

        idField.Text = towerConfig.Id;
        nameField.Text = towerConfig.Name;
        classField.Text = towerConfig.ClassName;
        bulletClassField.Text = towerConfig.BulletClassName;
        costField.Text = towerConfig.Cost.ToString();
        rangeField.Text = towerConfig.Range.ToString();
        fireRateField.Text = towerConfig.FireRate.ToString();
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
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

        saveButton.Update(mouse);
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        if (!IsOpen) return;

        int panelWidth = width;
        int panelHeight = height;
        int x = left;
        int y = top;

        // Фон панели
        sb.Draw(pixel, new Rectangle(x, y, panelWidth, panelHeight), Color.Black * 0.85f);
        selectorField.Draw(sb, font, pixel);

        int labelOffset = inputHeight;
        y += dropListHeight;

        DrawLabel(sb, font, "Id:", new Vector2(x + 4, y + 4));
        idField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Name:", new Vector2(x + 4, y + 4 + labelOffset * 1));
        nameField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Class name:", new Vector2(x + 4, y + 4 + labelOffset * 2));
        classField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Bullet class name:", new Vector2(x + 4, y + 4 + labelOffset * 3));
        bulletClassField.Draw(sb, font, pixel);


        DrawLabel(sb, font, "Cost:", new Vector2(x + 4, y + 4 + labelOffset * 4));
        costField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Range:", new Vector2(x + 4, y + 4 + labelOffset * 5));
        rangeField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Fire Rate:", new Vector2(x + 4, y + 4 + labelOffset * 6));
        fireRateField.Draw(sb, font, pixel);

        saveButton.Draw(sb, font, pixel);
    }

    private void DrawLabel(SpriteBatch sb, SpriteFont font, string text, Vector2 pos)
    {
        sb.DrawString(font, text, pos, Color.White);
    }

    private void SaveTower()
    {

        // Сохраняем только JSON конфиг в Content/Towers/
        // .cs файлы создавать не нужно - они уже есть в Types/
        string configDir = "Content/Towers";
        System.IO.Directory.CreateDirectory(configDir);
        
        string configPath = System.IO.Path.Combine(configDir, $"{towerConfig.Id}.json");
        
        var config = new {
            Id = towerConfig.Id,
            Name = towerConfig.Name,
            ClassName = towerConfig.ClassName,
            BulletClassName = towerConfig.BulletClassName,
            Cost = towerConfig.Cost,
            Range = towerConfig.Range,
            FireRate = towerConfig.FireRate,
        };
        
        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        string json = System.Text.Json.JsonSerializer.Serialize(config, options);
        System.IO.File.WriteAllText(configPath, json);
        
        Console.WriteLine($"Tower config saved: {configPath}");
    }
}
