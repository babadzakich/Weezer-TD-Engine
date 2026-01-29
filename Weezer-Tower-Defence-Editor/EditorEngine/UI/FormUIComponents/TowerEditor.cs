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

public class TowerEditor : IShowable
{
    private readonly int top = 0, left = 0, width = 1200, height = 1000;
    private readonly int form_width = 300, form_height=650;

    private readonly TextField nameLabel, numLevelsLabel;
    private readonly InputField nameField, numLevelsField;
    private readonly SelectorFieldForm towerSelector;
    private readonly ButtonField saveButton;

    public bool isShown = false;
    private List<ArgConfig> arguments = new();
    private readonly int form_top;
    private string name = "";
    private string selectedDamageDealer = "";

    private List<List<ArgValueSpec>> levels = new();
    private List<FormComponent> levelForm = new();

    public TowerEditor()
    {
        var names = TowerRegistry.Instance
            .behaviorDescriptions
            .Values
            .Select(x => x.Name)
            .ToList();

        var current_top = top;
        nameLabel = new TextField(current_top + 10, left + 10, "Tower Name:");
        current_top += 40;
        nameField = new InputField(current_top + 10, left + 10, width-20, 40, "nameField", "string", onNameUpdate, "");
        current_top += 60;

        numLevelsLabel = new TextField(current_top + 10, left + 10, "Number of levels:");
        current_top += 40;
        numLevelsField = new InputField(current_top + 10, left + 10, width - 20, 40, "numLevelsField", "int", onNumLevesUpdate, "");
        current_top += 60;

        towerSelector = new SelectorFieldForm(current_top + 10, left + 10, width-20, 40, "tower", names, onSelect);
        current_top += 60 * names.Count;

        
        form_top = current_top;

        saveButton = new ButtonField(current_top + 10 +form_height, left + 10, width - 20, 40, "Save Tower", onSaveClickButton);

    }

    private void onSelect(string selected, string id)
    {
        selectedDamageDealer = selected;
        var behaviorConfig = TowerRegistry.Instance
            .behaviorDescriptions
            .Values
            .FirstOrDefault(x => x.Name == selected);
        if (behaviorConfig != null)
        {
            arguments = behaviorConfig.Args;

            levels = Enumerable
                .Range(0, levelForm.Count)
                .Select(_ => new List<ArgValueSpec>())
                .ToList();
            for (int i = 0; i < levelForm.Count; i++)
            {
                levelForm[i] = new FormComponent(form_top, left + form_width*i, form_width, form_height, $"{i }", arguments, onSaveClick);
            }
        }
    }

    private void onNameUpdate(string newName, string id, string type)
    {
        name = newName;
    }

    private void onNumLevesUpdate(string newLevels, string id, string type)
    {
        if (int.TryParse(newLevels, out int levelsCount))
        {
            levels = Enumerable
                .Range(0, levelsCount)
                .Select(_ => new List<ArgValueSpec>())
                .ToList();
            levelForm = new List<FormComponent>();
            for (int i = 0; i < levelsCount; i++)
            {
                levelForm.Add(new FormComponent(form_top, left + form_width*i, form_width, form_height, $"{i}", arguments, onSaveClick));
            }
        }
    }

    private void onSaveClick(List<ArgValueSpec> argValues, string id)
    {
        Console.WriteLine($"Saving level {id} config; Total levels {levels.Count}");
        int.TryParse(id, out int levelIndex);
        levels[levelIndex] = argValues;
    }

    private void onSaveClickButton()
    {
        var className = TowerRegistry.Instance
            .behaviorDescriptions
            .Values
            .FirstOrDefault(x => x.Name == selectedDamageDealer)?
            .ClassName ?? "";


        var obj = new List<TypeSpecification>();
        foreach (var argValue in levels) 
        {
            obj.Add(new TypeSpecification() { Name = name, ClassName = className, Args = argValue });
        }


        var jsonRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom",
            "towers",
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
        nameLabel.Update(mouse, keyboard);
        nameField.Update(mouse, keyboard);
        numLevelsLabel.Update(mouse, keyboard);
        numLevelsField.Update(mouse, keyboard);
        towerSelector.Update(mouse, keyboard);
        saveButton.Update(mouse, keyboard);

        for(var i = 0; i < levelForm.Count; i++)
        {
            levelForm[i].Update(mouse, keyboard);
        }
    }


    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        sb.Draw(pixel, new Rectangle(left, top, width, height), new Color(40, 40, 60, 220));

        nameLabel.Draw(sb, font, pixel);
        nameField.Draw(sb, font, pixel);
        numLevelsLabel.Draw(sb, font, pixel);
        numLevelsField.Draw(sb, font, pixel);
        towerSelector.Draw(sb, font, pixel);
        saveButton.Draw(sb, font, pixel);

        for (var i = 0; i < levelForm.Count; i++)
        {
            levelForm[i].Draw(sb, font, pixel);
        }
    }

    public void Toggle() 
    { 
        isShown = !isShown;
    }

    public bool IsAnyFieldActive()
    {
        if (nameField.IsAnyFieldActive()) return true;
        if (numLevelsField.IsAnyFieldActive()) return true;
        for(var i = 0; i < levelForm.Count; i++)
        {
            if (levelForm[i].IsAnyFieldActive()) return true;
        }

        return false;
    }
}
