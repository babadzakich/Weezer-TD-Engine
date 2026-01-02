﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main()
        {
            Console.WriteLine("Starting Weezer Tower Defence...");
            
            using var game = new Game1();
            game.Run();
        }
    }
}
