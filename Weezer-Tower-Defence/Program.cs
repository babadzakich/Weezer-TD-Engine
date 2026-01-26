using System;
using System.Reflection;
﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using SimulationEngine.BulletRelated.Behaviors;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting Weezer Tower Defence...");


            // путь к твоей скомпиленной DLL
            string dllPath = @"C:\Users\vanam\AppData\Roaming\WeezerTowerDefence\DLLs\towers\BasicTower.dll";

            // грузим сборку
            Assembly assembly = Assembly.LoadFrom(dllPath);

            Type type = assembly.GetType("BasicTowerBehavior");

            // создаём экземпляр
            dynamic obj = Activator.CreateInstance(type, "hello", "bitch", new StandardBulletBehavior(10, 10, 10), 10, 10, 10)!;
            Console.WriteLine(obj.Name);


            using var game = new Game1();
            game.Run();
        }
    }
}
