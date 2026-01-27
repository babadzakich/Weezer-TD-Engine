using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using EditorEngine.Towers;
using EditorEngine.UI.FormUIComponents;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;

namespace EditorEngine.UI;

public class DamageDealerEditor : IShowable
{
    private readonly int top = 50, left = 50, width = 500, height = 800;

    private readonly InputField nameField;
    private readonly SelectorFieldForm damageDealerSelector;
    private FormComponent form;

    public bool isShown = false;
    private List<ArgConfig> arguments = new();
    private readonly int form_top;
    private string name = "";
    private string selectedDamageDealer = "";

    public DamageDealerEditor()
    {
        var damageDealerNames = DamageDealers.DamageDealerRegistry.Instance
            .behaviorDescriptions
            .Values
            .Select(x => x.Name)
            .ToList();

        var current_top = top;
        nameField = new InputField(current_top + 10, left + 10, width-20, 40, "nameField", "string", onNameUpdate, "");
        current_top += 60;
        damageDealerSelector = new SelectorFieldForm(current_top + 10, left + 10, width-20, 40, "damageDealer", damageDealerNames, onDamageDealerSelect);
        current_top += 60;
        form = new FormComponent(current_top, left, width, height, "Damage Dealer Editor", arguments, onSaveClick);
        form_top = current_top;

    }

    private void onDamageDealerSelect(string selected, string id)
    {
        selectedDamageDealer = selected;
        var behaviorConfig = DamageDealers.DamageDealerRegistry.Instance
            .behaviorDescriptions
            .Values
            .FirstOrDefault(x => x.Name == selected);
        if (behaviorConfig != null)
        {
            arguments = behaviorConfig.Args;
            form = new FormComponent(form_top, left, width, height, "Damage Dealer Editor", arguments, onSaveClick);
        }
    }

    private void onNameUpdate(string newName, string id, string type)
    {
        name = newName;
    }

    private void onSaveClick(List<ArgValueSpec> argValues, string id)
    {
        var className = DamageDealers.DamageDealerRegistry.Instance
            .behaviorDescriptions
            .Values
            .FirstOrDefault(x => x.Name == selectedDamageDealer)?
            .ClassName ?? "";

        var obj = new TypeSpecification() { Name = name, ClassName = className, Args = argValues };

        var jsonRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom",
            "damageDealers",
            "configs"
        );


        System.IO.Directory.CreateDirectory(jsonRoot);
        var jsonPath = System.IO.Path.Combine(jsonRoot, $"{name}.json");
        Console.WriteLine($"Saving Damage Dealer config to: {jsonPath}");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var jsonString = JsonSerializer.Serialize(obj, options);

        File.WriteAllText(jsonPath, jsonString);
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        nameField.Update(mouse, keyboard);
        damageDealerSelector.Update(mouse, keyboard);
        form.Update(mouse, keyboard);
    }


    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        sb.Draw(pixel, new Rectangle(left, top, width, height), new Color(40, 40, 60, 220));

        nameField.Draw(sb, font, pixel);
        damageDealerSelector.Draw(sb, font, pixel);
        form.Draw(sb, font, pixel);
    }

    public void Toggle() 
    { 
        isShown = !isShown;
    }

    public bool IsAnyFieldActive()
    {
        if (nameField.IsAnyFieldActive()) return true;
        if (damageDealerSelector.IsAnyFieldActive()) return true;
        if (form.IsAnyFieldActive()) return true;

        return false;
    }
}
