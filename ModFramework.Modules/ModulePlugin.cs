﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using ModFramework.Plugins;
using ModFramework.Relinker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ModFramework.Modules
{
    [MonoMod.MonoModIgnore]
    [Modification(ModType.Read, "Loading CSharpScript interface")]
    public class ModulePlugin
    {
        const string ConsolePrefix = "CSharp";
        const string ModulePrefix = "CSharpScript_";
        public MonoMod.MonoModder Modder { get; set; }

        public ModulePlugin(MonoMod.MonoModder modder)
        {
            Modder = modder;

            Console.WriteLine($"[{ConsolePrefix}] Starting runtime");

            modder.OnReadMod += (m, module) =>
            {
                if (module.Assembly.Name.Name.StartsWith(ModulePrefix))
                {
                    // remove the top level program class
                    var tlc = module.GetType("<Program>$");
                    if (tlc != null)
                    {
                        module.Types.Remove(tlc);
                    }
                    Modder.RelinkAssembly(module);
                }
            };

            RunModules();
        }

        void RunModules()
        {
            var path = Path.Combine("csharp", "modifications");
            var outDir = Path.Combine("csharp", "generated");
            if (Directory.Exists(path))
            {
                if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
                Directory.CreateDirectory(outDir);

                var constants = File.ReadAllText("../../../../OTAPI.Setup/bin/Debug/net5.0/AutoGenerated.cs"); // bring across the generated constants

                foreach (var file in Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
                {
                    Console.WriteLine($"[{ConsolePrefix}] Loading module: {file}");
                    try
                    {
                        var encoding = System.Text.Encoding.UTF8;
                        var options = CSharpParseOptions.Default
                               .WithLanguageVersion(LanguageVersion.Preview); // allows toplevel functions

                        SyntaxTree encoded;
                        SourceText source;
                        using (var stream = File.OpenRead(file))
                        {
                            source = SourceText.From(stream, encoding, canBeEmbedded: true);
                            encoded = CSharpSyntaxTree.ParseText(source, options, file);
                        }

                        using var dllStream = new MemoryStream();
                        using var pdbStream = new MemoryStream();
                        using var xmlStream = new MemoryStream();

                        var assemblyName = $"{ModulePrefix}{Guid.NewGuid():N}";

                        var outAsmPath = Path.Combine(outDir, $"{assemblyName}.dll");
                        var outPdbPath = Path.Combine(outDir, $"{assemblyName}.pdb");

                        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

                        EmitResult Compile(bool dll)
                        {
                            var compilation = CSharpCompilation.Create(assemblyName, new[] { encoded }, new[]
                            {
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Private.CoreLib.dll")),
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Console.dll")),
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll")),
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Collections.dll")),
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.dll")),
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Linq.Expressions.dll")),
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Threading.Thread.dll")),
                                MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
                                MetadataReference.CreateFromFile(typeof(ModType).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(Mono.Cecil.AssemblyDefinition).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(Newtonsoft.Json.JsonConvert).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(IRelinkProvider).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(MonoMod.MonoModder).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(Terraria.WindowsLaunch).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(System.Runtime.InteropServices.RuntimeInformation).Assembly.Location)
                            }
                                .Concat(typeof(MonoMod.MonoModder).Assembly.GetReferencedAssemblies()
                                .Select(asm => MetadataReference.CreateFromFile(Assembly.Load(asm).Location)))
                            );

                            if (dll)
                            {
                                compilation = compilation
                                    .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                                    .WithOptimizationLevel(OptimizationLevel.Debug)
                                    .WithPlatform(Platform.AnyCpu));
                            }

                            var emitOptions = new EmitOptions(
                                    debugInformationFormat: DebugInformationFormat.PortablePdb,
                                    pdbFilePath: outPdbPath);

                            var embeddedTexts = new List<EmbeddedText>
                            {
                                EmbeddedText.FromSource(file, source),
                            };

                            EmitResult result = compilation.Emit(
                                peStream: dllStream,
                                pdbStream: pdbStream,
                                embeddedTexts: embeddedTexts,
                                options: emitOptions);

                            return result;
                        }

                        var compilationResult = Compile(false);

                        if (!compilationResult.Success)
                        {
                            compilationResult = Compile(true); // usually a monomod patch
                        }

                        if (compilationResult.Success)
                        {
                            // save the file for monomod (doesnt like streams it seems?)
                            // then register the reflected assembly, then the monomod variant for patches

                            dllStream.Seek(0, SeekOrigin.Begin);
                            pdbStream.Seek(0, SeekOrigin.Begin);

                            var asm = PluginLoader.AssemblyLoader.Load(dllStream, pdbStream);
                            PluginLoader.AddAssembly(asm);

                            File.WriteAllBytes(outAsmPath, dllStream.ToArray());
                            File.WriteAllBytes(outPdbPath, pdbStream.ToArray());

                            Modder.ReadMod(outAsmPath);
                        }
                        else
                        {
                            Console.WriteLine($"Compilation errors for file: {Path.GetFileName(file)}");

                            foreach (var diagnostic in compilationResult.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error))
                            {
                                Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{ConsolePrefix}] Load error: {ex}");
                    }
                }
            }
        }
    }
}
