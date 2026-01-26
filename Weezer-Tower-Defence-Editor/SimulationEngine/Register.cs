using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

class Register { 
    public static void setup() {
        Console.WriteLine("Setting up");
        copyInbuilt();
        createDLLs();
    }

    /// <summary>
    /// This function is needed to copy all inbuilt behaviors to the appdata
    /// 
    /// In the following, we will work with user-defined behaviors which he should implement in appdata, 
    /// therefore, to gain consistnecy and simplicity we copy all inbuilt behaviors to appdata as well
    /// </summary>
    private static void copyInbuilt()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var targetRoot = Path.Combine(
            appData,
            "WeezerTowerDefence",
            "Editor",
            "custom"
        );

        recursiveCopy(
            sourceDir: "EmbeddedBehaviors",
            targetDir: targetRoot
        );
    }

    private static void copyDlls()     {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var targetRoot = Path.Combine(
            appData,
            "WeezerTowerDefence",
            "Editor",
            "DLLs"
        );
        recursiveCopy(
            sourceDir: "PrecompiledDLLs",
            targetDir: targetRoot
        );
        Console.WriteLine("NU I GDE");
    }

    private static void recursiveCopy(string sourceDir, string targetDir)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyName = assembly.GetName().Name!;

        var resourceRoot = $"{assemblyName}.{sourceDir}.";

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            Console.WriteLine($"{resourceName};   {resourceRoot}");
            if (!resourceName.StartsWith(resourceRoot))
                continue;

            Console.WriteLine("WOW");
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

            var references = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a =>
                    !a.IsDynamic &&
                    !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                Path.GetFileNameWithoutExtension(outputDllPath),
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            Directory.CreateDirectory(Path.GetDirectoryName(outputDllPath)!);

            var result = compilation.Emit(outputDllPath);

            if (!result.Success)
            {
                var errors = string.Join(
                    "\n",
                    result.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .Select(d => d.ToString())
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
