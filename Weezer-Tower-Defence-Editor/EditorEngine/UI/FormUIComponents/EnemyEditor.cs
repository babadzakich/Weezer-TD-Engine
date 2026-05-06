using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EditorEngine.UI.FormUIComponents;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.Infrastructure;

namespace EditorEngine.UI;

public class EnemyEditor : IShowable
{
    private readonly int top = 50, left = 50, width = 500, height = 800;

    private readonly InputField nameField;
    private readonly SelectorFieldForm enemySelector;
    private FormComponent form;

    public bool isShown = false;
    private List<ArgConfig> arguments = new();
    private readonly int form_top;
    private string name = "";
    private string selectedDamageDealer = "";

    public EnemyEditor()
    {
        var damageDealerNames = EnemyRegistry.Instance
            .behaviorDescriptions
            .Values
            .Select(x => x.Name)
            .ToList();

        var current_top = top;
        nameField = new InputField(current_top + 10, left + 10, width-20, 40, "nameField", "string", onNameUpdate, "");
        current_top += 60;
        enemySelector = new SelectorFieldForm(current_top + 10, left + 10, width-20, 40, "enemy", damageDealerNames, onDamageDealerSelect);
        current_top += 60 * damageDealerNames.Count;
        form = new FormComponent(current_top, left, width, height, "Enemy Editor", arguments, onSaveClick);
        form_top = current_top;

    }

    private void onDamageDealerSelect(string selected, string id)
    {
        selectedDamageDealer = selected;
        var behaviorConfig = EnemyRegistry.Instance
            .behaviorDescriptions
            .Values
            .FirstOrDefault(x => x.Name == selected);
        if (behaviorConfig != null)
        {
            arguments = behaviorConfig.Args;
            form = new FormComponent(form_top, left, width, height, "Enemy Editor", arguments, onSaveClick);
        }
    }

    private void onNameUpdate(string newName, string id, string type)
    {
        name = newName;
    }

    private void onSaveClick(List<ArgValueSpec> argValues, string id)
    {
        var className = EnemyRegistry.Instance
            .behaviorDescriptions
            .Values
            .FirstOrDefault(x => x.Name == selectedDamageDealer)?
            .ClassName ?? "";

        var obj = new TypeSpecification() { Name = name, ClassName = className, Args = argValues };

        var jsonRoot = PathService.GetEditorConfigDirectory("enemies");


        System.IO.Directory.CreateDirectory(jsonRoot);
        var jsonPath = System.IO.Path.Combine(jsonRoot, $"{name}.json");
        Console.WriteLine($"Saving Enemy config to: {jsonPath}");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var jsonString = JsonSerializer.Serialize(obj, options);

        File.WriteAllText(jsonPath, jsonString);

        EnemyRegistry.Instance.Update();
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        nameField.Update(mouse, keyboard);
        enemySelector.Update(mouse, keyboard);
        form.Update(mouse, keyboard);
    }


    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        sb.Draw(pixel, new Rectangle(left, top, width, height), new Color(40, 40, 60, 220));

        nameField.Draw(sb, font, pixel);
        enemySelector.Draw(sb, font, pixel);
        form.Draw(sb, font, pixel);
    }

    public void Toggle() 
    { 
        isShown = !isShown;
    }

    public bool IsAnyFieldActive()
    {
        if (nameField.IsAnyFieldActive()) return true;
        if (enemySelector.IsAnyFieldActive()) return true;
        if (form.IsAnyFieldActive()) return true;

        return false;
    }
}
