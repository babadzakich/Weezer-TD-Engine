using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SimulationEngine.Infrastructure;

namespace EditorEngine
{
    public static class DllBuilder
    {
        public static void BuildAll()
        {
            RecreateDllDirectories();

            string editorRoot = PathService.EditorDirectory;
            var csFiles = Directory.GetFiles(editorRoot, "*.cs", SearchOption.AllDirectories);
            var errors = new List<string>();

            foreach (var csFile in csFiles)
            {
                try
                {
                    var relativePath = Path.GetRelativePath(editorRoot, csFile);
                    var parts = relativePath.Split(Path.DirectorySeparatorChar);

                    if (parts.Length < 2)
                    {
                        errors.Add($"Broken filename structure: {relativePath}");
                        continue;
                    }

                    string entityType = parts[0];
                    string fileName = Path.GetFileNameWithoutExtension(csFile);
                    string outputDir = PathService.GetEntityDllDirectory(entityType);
                    string outputDllPath = Path.Combine(outputDir, $"{fileName}.dll");

                    CompileSingleFileToDll(csFile, outputDllPath);
                    Console.WriteLine($"Compiled {entityType}/{fileName}.dll");
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to compile {csFile}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
                throw new Exception(string.Join("\n", errors));
        }

        private static void RecreateDllDirectories()
        {
            if (Directory.Exists(PathService.DLLsDirectory))
            {
                try
                {
                    Directory.Delete(PathService.DLLsDirectory, recursive: true);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"Warning: Could not fully clean DLLs directory: {ex.Message}");
                }
            }

            Directory.CreateDirectory(PathService.DLLsDirectory);

            foreach (var entityType in new[] { "towers", "enemies", "damageDealers" })
                Directory.CreateDirectory(PathService.GetEntityDllDirectory(entityType));
        }

        private static void CompileSingleFileToDll(string sourcePath, string outputDllPath)
        {
            string code = File.ReadAllText(sourcePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = new List<MetadataReference>();
            var tpaData = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
                ?? throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is unavailable.");
            var tpa = tpaData.Split(Path.PathSeparator);

            references.AddRange(tpa.Select(p => MetadataReference.CreateFromFile(p)));

            var extraRefs = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a =>
                    a.GetName().Name!.StartsWith("MonoGame") ||
                    a.GetName().Name == "Shared_simulation_engine")
                .Where(a => !a.IsDynamic && File.Exists(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location));

            references.AddRange(extraRefs);

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
                    result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
                );

                throw new Exception(errors);
            }
        }
    }
}
