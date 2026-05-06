using System;

namespace Weezer_Tower_Defence
{
    public static class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting Weezer Tower Defence Editor...");

            Register.setup();
            using var game = new Editor();
            game.Run();
        }
    }
}
