using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EditorEngine.DamageDealers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimulationEngine.Infrastructure;

class Register
{
    public static void setup()
    {
        Console.WriteLine("Setting up");
        ensureEditorDirectories();
        copyInbuilt();
        copySharedContent();
        createDLLs();
        var tmp_enemies = EnemyRegistry.Instance;
        var tmp = DamageDealerRegistry.Instance;
        var tmp_towers = TowerRegistry.Instance;
    }

    /// <summary>
    /// This function is needed to copy all inbuilt behaviors to the appdata
    /// 
    /// In the following, we will work with user-defined behaviors which he should implement in appdata, 
    /// therefore, to gain consistnecy and simplicity we copy all inbuilt behaviors to appdata as well
    /// </summary>
    private static void copyInbuilt()
    {
        var targetRoot = PathService.EditorDirectory;

        Directory.CreateDirectory(targetRoot);

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

    private static void ensureEditorDirectories()
    {
        var editorRoot = PathService.EditorDirectory;
        var dllRoot = PathService.DLLsDirectory;

        string[] relativeDirs =
        {
            Path.Combine("towers", "behaviors"),
            Path.Combine("towers", "configs"),
            Path.Combine("enemies", "behaviors"),
            Path.Combine("enemies", "configs"),
            Path.Combine("damageDealers", "behaviors"),
            Path.Combine("damageDealers", "configs")
        };

        Directory.CreateDirectory(editorRoot);
        Directory.CreateDirectory(dllRoot);

        foreach (var dir in relativeDirs)
        {
            Directory.CreateDirectory(Path.Combine(editorRoot, dir));
            Directory.CreateDirectory(Path.Combine(dllRoot, dir.Split(Path.DirectorySeparatorChar)[0]));
        }
    }

    private static void recursiveCopy(string sourceDir, string targetDir)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name!;

        var resourceRoot = $"{assemblyName}.{sourceDir}.";

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            Console.WriteLine($"Copying resource {resourceName}");
            if (!resourceName.StartsWith(resourceRoot))
                continue;

            var relative = resourceName.Substring(resourceRoot.Length);

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


    /// <summary>
    /// All possible classes are in appdata/WeezerTowerDefence/Editor/custom
    /// 
    /// We want to compile them into DLLs and put into appdata/WeezerTowerDefence/DLLs
    /// </summary>
    private static void createDLLs()
    {
        void CompileSingleFileToDll(string sourcePath, string outputDllPath)
        {
            string code = File.ReadAllText(sourcePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // 1️⃣ Базовые .NET сборки
            var references = new List<MetadataReference>();
            var tpa = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
                .Split(Path.PathSeparator);

            references.AddRange(
                tpa.Select(p => MetadataReference.CreateFromFile(p))
            );

            // 2️⃣ Подтягиваем MonoGame и Shared_simulation_engine из AppDomain
            var extraRefs = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                    a.GetName().Name.StartsWith("MonoGame") ||
                    a.GetName().Name == "Shared_simulation_engine"
                )
                .Where(a => !a.IsDynamic && File.Exists(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));

            references.AddRange(extraRefs);

            // 3️⃣ Компиляция DLL
            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputDllPath),
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release
                )
            );

            Directory.CreateDirectory(Path.GetDirectoryName(outputDllPath)!);

            var result = compilation.Emit(outputDllPath);

            if (!result.Success)
            {
                var errors = string.Join(
                    "\n",
                    result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                );

                throw new Exception(errors);
            }
        }

        string editorCustomPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "Editor",
            "custom"
        );

        string dllRootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WeezerTowerDefence",
            "DLLs"
        );

        var csFiles = Directory.GetFiles(
            editorCustomPath,
            "*.cs",
            SearchOption.AllDirectories
        );

        foreach (var csFile in csFiles)
        {
            try
            {
                var relativePath = Path.GetRelativePath(editorCustomPath, csFile);
                var parts = relativePath.Split(Path.DirectorySeparatorChar);

                if (parts.Length < 2)
                {
                    Console.WriteLine($"Broken filename structure: {relativePath}");
                    continue;
                }

                string entityType = parts[0];
                string fileName = Path.GetFileNameWithoutExtension(csFile);

                string outputDir = Path.Combine(dllRootPath, entityType);
                string outputDllPath = Path.Combine(outputDir, $"{fileName}.dll");

                CompileSingleFileToDll(csFile, outputDllPath);

                Console.WriteLine($"Compiled {entityType}/{fileName}.dll");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to compile {csFile}: {ex.Message}");
            }
        }
    }
}
