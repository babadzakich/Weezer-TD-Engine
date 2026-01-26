﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using EditorEngine;
using System;

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
