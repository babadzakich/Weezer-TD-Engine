using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimulationEngine.BulletRelated.Behaviors;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main()
        {
            //Console.WriteLine("Starting Weezer Tower Defence...");


            //// путь к твоей скомпиленной DLL
            //string dllPath = @"C:\Users\vanam\AppData\Roaming\WeezerTowerDefence\DLLs\damageDealers\standardBullet.dll";

            //// грузим сборку
            //Assembly assembly = Assembly.LoadFrom(dllPath);

            //var types = assembly.GetTypes().ToList();
            //for (var typel in types)
            //{ }


            //Type type = assembly.GetType("StandardBulletBehavior");

            //// создаём экземпляр
            //dynamic obj = Activator.CreateInstance(type, "hello", "bitch", new StandardBulletBehavior(10, 10, 10), 10, 10, 10)!;
            //Console.WriteLine(obj.Name);


            using var game = new Game1();
            game.Run();
        }
    }
}
