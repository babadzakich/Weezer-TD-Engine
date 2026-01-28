using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EditorEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using EditorEngine.DamageDealers;
using SimulationEngine;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Weezer Tower Defence...");

            Register.setup();
            using var game = new Editor();
            game.Run();
        }
    }
}
