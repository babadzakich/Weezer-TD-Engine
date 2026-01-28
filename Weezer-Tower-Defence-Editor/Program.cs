using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using EditorEngine;
using EditorEngine.DamageDealers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Weezer Tower Defence...");


            //string dllPath = @"C:\Users\vanam\AppData\Roaming\WeezerTowerDefence\DLLs\damageDealers\standardBullet.dll";

            var jsonRoot = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom",
            "damageDealers",
            "behaviors"
        );
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            if (!Directory.Exists(jsonRoot))
                throw new DirectoryNotFoundException(jsonRoot);

            foreach (var jsonPath in Directory.EnumerateFiles(jsonRoot, "*.json"))
            {
                var json = File.ReadAllText(jsonPath);

                var config = JsonSerializer.Deserialize<BehaviorConfig>(json, jsonOptions);
                if (config == null)
                    throw new Exception($"Failed to parse {jsonPath}");

                var dllPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WeezerTowerDefence",
                    "DLLs",
                    "damageDealers",
                    $"{config.FileName}.dll"
                 );

                if (!File.Exists(dllPath))
                    throw new FileNotFoundException(dllPath);

                var assembly = Assembly.LoadFrom(dllPath);

                var type = assembly
                    .GetTypes()
                    .FirstOrDefault(t => t.Name == config.ClassName);

                Console.WriteLine($"OFF COURSE IT IS NOT NONE: {type.Name}");

                if (type == null)
                    throw new Exception(
                        $"Type {config.ClassName} not found in {dllPath}"
                    );
            }
        }
    }
}
