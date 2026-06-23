using System;
using System.IO;

namespace SimulationEngine.Network;

public static class OwnershipDebug
{
    private static readonly string LogPath = @"C:\Projects\HW\Weezer-TD-Engine\ownership_debug.txt";
    private static readonly object LockObj = new object();

    public static void Clear()
    {
        try
        {
            if (File.Exists(LogPath))
            {
                File.Delete(LogPath);
            }
        }
        catch { }
    }

    public static void Log(string message)
    {
        for (int i = 0; i < 10; i++)
        {
            try
            {
                lock (LockObj)
                {
                    File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] [PID {Environment.ProcessId}] {message}\n");
                }
                break;
            }
            catch
            {
                System.Threading.Thread.Sleep(20);
            }
        }
    }
}
