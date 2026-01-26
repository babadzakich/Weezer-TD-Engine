using System;
using System.Xml.Linq;
using EditorEngine.Towers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

namespace EditorEngine.UI;

public class  MoneyHealthEditor
{
    private int y_offset = 300;
    private int x_offset = 20;
    private int label_offset = 90;
    private int field_offset = 80;

    private int money = 500;
    private int health = 100;

    private readonly UITextField moneyField;
    private readonly UITextField healthField;
    private readonly UIButton saveButton;

    public MoneyHealthEditor()
    {
        int x = x_offset+ 10, y = y_offset + 10, w = 50, h = 30;
        moneyField = new(new Rectangle(x_offset + label_offset, y_offset, w, h), money.ToString(), (string x, string y) => { }, "money");
        healthField = new(new Rectangle(x_offset + label_offset, y_offset + field_offset, w, h), health.ToString(), (x, y) => { }, "health");

        saveButton = new UIButton(
            new Rectangle(x_offset, y_offset + field_offset * 2, 350, h),
            "save money and health",
            SaveMoneyHealth
        );
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        moneyField.Update(mouse, keyboard);
        healthField.Update(mouse, keyboard);

        saveButton.Update(mouse);
    }

    public bool IsAnyFieldActive()
    {
        return moneyField.IsActive || healthField.IsActive;
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        int x = x_offset+10;
        int y = y_offset+10;


        DrawLabel(sb, font, "Money:", new Vector2(x_offset, y_offset));
        moneyField.Draw(sb, font, pixel);

        DrawLabel(sb, font, "Health:", new Vector2(x_offset, y_offset + field_offset));
        healthField.Draw(sb, font, pixel);

        saveButton.Draw(sb, font, pixel);
    }

    private void DrawLabel(SpriteBatch sb, SpriteFont font, string text, Vector2 pos)
    {
        sb.DrawString(font, text, pos, Color.White);
    }

    private void SaveMoneyHealth()
    {
        if (int.TryParse(moneyField.Text, out int parsedMoney))
            money = parsedMoney;

        if (int.TryParse(healthField.Text, out int parsedHealth))
            health = parsedHealth;


        string configDir = "Content/";
        System.IO.Directory.CreateDirectory(configDir);
        string configPath = System.IO.Path.Combine(configDir, $"MoneyHealth.json");

        var config = new
        {
            StartingMoney = money,
            StartingLives = health
        };

        var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
        string json = System.Text.Json.JsonSerializer.Serialize(config, options);
        System.IO.File.WriteAllText(configPath, json);

        Console.WriteLine($"Money/health config saved: {configPath}");
    }
}
