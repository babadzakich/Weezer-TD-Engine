using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EditorEngine.DamageDealers;
using SimulationEngine.Infrastructure;

class Register
{
    public static void setup()
    {
        Console.WriteLine("Setting up");
        PathService.EnsureInitialized();
        copyInbuilt();
        copySharedContent();
        var tmp_enemies = EnemyRegistry.Instance;
        var tmp = DamageDealerRegistry.Instance;
        var tmp_towers = TowerRegistry.Instance;
    }

    /// <summary>
    /// Copies built-in editor templates into AppData so the editor always works
    /// with a single writable source of truth.
    /// </summary>
    private static void copyInbuilt()
    {
        var targetRoot = PathService.EditorDirectory;

        Console.WriteLine(targetRoot);
        recursiveCopy(
            sourceDir: "EmbeddedBehaviors",
            targetDir: targetRoot
        );
    }

    private static void copySharedContent()
    {
        var targetRoot = PathService.CommonDirectory;

        try
        {
            Directory.CreateDirectory(targetRoot);

            recursiveCopy(
                sourceDir: "Content",
                targetDir: targetRoot
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to copy shared content to {targetRoot}: {ex.Message}");
        }
    }

    private static void recursiveCopy(string sourceDir, string targetDir)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceMarker = $".{sourceDir}.";

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            Console.WriteLine($"Copying resource {resourceName}");
            int markerIndex = resourceName.IndexOf(resourceMarker, StringComparison.Ordinal);
            if (markerIndex < 0)
                continue;

            var relative = resourceName.Substring(markerIndex + resourceMarker.Length);

            var parts = relative.Split('.');

            if (parts.Length < 2)
                continue;

            var fileName = $"{parts[^2]}.{parts[^1]}";
            var directories = parts.Take(parts.Length - 2);

            var relativePath = Path.Combine(
                Path.Combine(directories.ToArray()),
                fileName
            );

            var targetPath = Path.Combine(targetDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Resource not found: {resourceName}");

            Console.WriteLine(targetPath);
            using var file = File.Create(targetPath);
            stream.CopyTo(file);
        }
    }
}
