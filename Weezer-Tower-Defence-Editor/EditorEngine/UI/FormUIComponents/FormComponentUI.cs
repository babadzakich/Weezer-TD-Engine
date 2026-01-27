using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EditorEngine.DamageDealers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace EditorEngine.UI.FormUIComponents;




public class FormComponent : IShowable
{

    private readonly List<ArgConfig> config;
    private readonly Action<List<ArgValueSpec>> onChange;


    private readonly int top, left, width, height;
    private readonly string id;
    private readonly Action<List<ArgValueSpec>, string> onSave;
    private readonly Dictionary<string, string> currentValues = new();
    private readonly List<IShowable> showables = new();
    


    public FormComponent(int top, int left, int width, int height, string id, List<ArgConfig> config, Action<List<ArgValueSpec>, string> onSave)
    {
        this.top = top;
        this.left = left;
        this.width = width;
        this.height = height;
        this.id = id;
        this.config = config;
        this.onSave = onSave;

        int current_top = this.top + 10;
        foreach (var arg in config) 
        {
            switch (arg.Type) 
            {
                case "int":
                    currentValues[arg.Name] = "0";
                    this.showables.Add(new TextField(current_top, left + 10, arg.Name));
                    current_top += 40;
                    this.showables.Add(new InputField(current_top, left + 10, width - 20, 40, arg.Name, arg.Type, onInputUpdate, ""));
                    current_top += 50;
                    break;
                case "float":
                    currentValues[arg.Name] = "0.0";
                    this.showables.Add(new TextField(current_top, left + 10, arg.Name));
                    current_top += 40;
                    this.showables.Add(new InputField(current_top, left + 10, width - 20, 40, arg.Name, arg.Type, onInputUpdate, ""));
                    current_top += 50;
                    break;
                case "string":
                    currentValues[arg.Name] = "";
                    this.showables.Add(new TextField(current_top, left + 10, arg.Name));
                    current_top += 40;
                    this.showables.Add(new InputField(current_top, left + 10, width - 20, 40, arg.Name, arg.Type, onInputUpdate, ""));
                    current_top += 50;
                    break;
                case "IDamageDealerBehavior":
                    var options = DamageDealerRegistry.Instance.damageDealers.Keys.ToList();
                    currentValues[arg.Name] = options[0];
                    this.showables.Add(new TextField(current_top, left + 10, arg.Name));
                    current_top += 40;
                    this.showables.Add(new SelectorFieldForm(current_top, left + 10, width - 20, 40, arg.Name, options, onSelection));
                    current_top += 50 * options.Count;
                    break;
                default:
                    throw new Exception($"Unsupported arg type in form: {arg.Type}");
            }
        }

        this.showables.Add(new ButtonField(current_top, left + 10, width - 20, 50, "Save", onSaveClick));
    }

    private void onInputUpdate(string value, string fieldId, string type) { 
        currentValues[fieldId] = value;
    }

    private void onSelection(string value, string id) 
    {
        currentValues[id] = value;
    }

    private void onSaveClick() 
    {
        var argValues = new List<ArgValueSpec>();
        foreach (var arg in config) 
        {
            if (currentValues.ContainsKey(arg.Name)) 
            {
                Console.WriteLine($"Fucken JSON token: {currentValues[arg.Name]}");
                string raw = currentValues[arg.Name];

                JsonElement value;

                if (string.IsNullOrWhiteSpace(raw))
                {
                    value = JsonDocument.Parse("null").RootElement;
                }
                else if (
                    raw.StartsWith("{") ||
                    raw.StartsWith("[") ||
                    raw == "true" ||
                    raw == "false" ||
                    raw == "null" ||
                    double.TryParse(raw, out _)
                )
                {
                    // уже похоже на JSON
                    value = JsonSerializer.Deserialize<JsonElement>(raw);
                }
                else
                {
                    // ОБЫЧНАЯ СТРОКА → заворачиваем в JSON
                    string jsonString = JsonSerializer.Serialize(raw);
                    value = JsonSerializer.Deserialize<JsonElement>(jsonString);
                }

                argValues.Add(new ArgValueSpec
                {
                    Name = arg.Name,
                    Value = value
                });

            }
            else 
            {
                return;
            }
        }
        onSave(argValues, id);
    }

    public void Update(MouseState mouse, KeyboardState keyboard)
    {
        foreach (var component in showables) 
        {
            component.Update(mouse, keyboard);
        }
    }

    public void Draw(SpriteBatch sb, SpriteFont font, Texture2D pixel)
    {
        foreach (var component in showables) 
        {
            component.Draw(sb, font, pixel);
        }
    }

    public bool IsAnyFieldActive() 
    {
        foreach(var showable in showables) 
        {
            if (showable.IsAnyFieldActive()) 
            {
                return true;
            }
        }

        return false;
    }
}
