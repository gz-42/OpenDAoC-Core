/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
#if NET
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DOL.GS
{
    public class DOLScriptCompiler
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private Compilation compiler;
        private Microsoft.CodeAnalysis.Emit.EmitResult lastEmitResult;
        private static List<PortableExecutableReference> referencedAssemblies;

        static DOLScriptCompiler()
        {
            referencedAssemblies = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .Where(a => !a.IsDynamic)
                                .Select(a => a.Location)
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Select(s => MetadataReference.CreateFromFile(s))
                                .ToList();

            var additionalReferences = GameServer.Instance.Configuration.AdditionalScriptAssemblies;

            foreach (var additionalReference in additionalReferences)
            {
                var dllName = additionalReference.EndsWith(".dll") ? additionalReference : additionalReference + ".dll";
                var probingPaths = new[] { ".", "lib", Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location) };
                var foundReference = false;
                foreach (var probingPath in probingPaths)
                {
                    var potentialReferenceFilePath = Path.Combine(probingPath, dllName);
                    if (File.Exists(potentialReferenceFilePath))
                    {
                        referencedAssemblies.Add(MetadataReference.CreateFromFile(potentialReferenceFilePath));
                        foundReference = true;
                        break;
                    }
                }
                if (foundReference == false) log.Error($"Reference not found: {additionalReference}");
            }
        }

        public bool HasErrors => !lastEmitResult.Success;

        public void SetToVisualBasicNet()
        {
            throw new NotSupportedException("Please migrate your scripts to C#.");
        }

        public Assembly Compile(FileInfo outputFile, IEnumerable<FileInfo> sourceFiles)
        {
            var syntaxTrees = sourceFiles.Where(file => file.Name != "AssemblyInfo.cs")
                .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file.FullName)));

            Compile(outputFile, syntaxTrees);
            return Assembly.LoadFrom(outputFile.FullName);
        }

        public Assembly CompileFromSource(string code)
        {
            var outputFile = new FileInfo("code_" + Guid.NewGuid() + ".dll");
            var syntaxTrees = new List<SyntaxTree>() { CSharpSyntaxTree.ParseText(code) };

            Compile(outputFile, syntaxTrees);
            if (HasErrors) return null;
            var assembly = Assembly.Load(File.ReadAllBytes(outputFile.FullName));
            File.Delete(outputFile.FullName);
            return assembly;
        }

        private void Compile(FileInfo outputFile, IEnumerable<SyntaxTree> syntaxTrees)
        {
            var compilerParameters = new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    warningLevel: 2);
            compiler = CSharpCompilation.Create(
                outputFile.Name,
                options: compilerParameters,
                references: referencedAssemblies,
                syntaxTrees: syntaxTrees);
            var emitResult = compiler.Emit(outputFile.FullName);
            GC.Collect();

            lastEmitResult = emitResult;
        }

        public IEnumerable<string> GetDetailedErrorMessages()
        {
            var errorDiagnostics = lastEmitResult.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

            var errorMessages = new List<string>();
            foreach (var diag in errorDiagnostics)
            {
                errorMessages.Add($"{diag.Location} {diag.Id}: {diag.GetMessage()}");
            }
            return errorMessages;
        }

        public IEnumerable<string> GetErrorMessages()
        {
            var errorDiagnostics = lastEmitResult.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);

            var errorMessages = new List<string>();
            foreach (var diag in errorDiagnostics)
            {
                errorMessages.Add(diag.GetMessage());
            }
            return errorMessages;
        }
    }
}

#elif NETFRAMEWORK
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.CSharp;
using Microsoft.VisualBasic;

namespace DOL.GS
{
    public class DOLScriptCompiler
    {
        private static readonly Logging.Logger log = Logging.LoggerFactory.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private CodeDomProvider compiler;
        private CompilerErrorCollection lastCompilationErrors;
        private static List<string> referencedAssemblies = new List<string>();

        static DOLScriptCompiler()
        {
            var libDirectory = new DirectoryInfo(Path.Combine(GameServer.Instance.Configuration.RootDirectory, "lib"));
            referencedAssemblies.AddRange(libDirectory.GetFiles("*.dll", SearchOption.TopDirectoryOnly).Select(f => f.Name));
            referencedAssemblies.Add("System.dll");
            referencedAssemblies.Add("System.Xml.dll");
            referencedAssemblies.Add("System.Core.dll");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                referencedAssemblies.Add("netstandard.dll");
            }

            referencedAssemblies.AddRange(GameServer.Instance.Configuration.AdditionalScriptAssemblies);
        }

        public DOLScriptCompiler()
        {
            compiler = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
        }

        public bool HasErrors => lastCompilationErrors.HasErrors;

        public void SetToVisualBasicNet()
        {
            compiler = new VBCodeProvider();
        }

        public Assembly Compile(FileInfo outputFile, IEnumerable<FileInfo> sourceFiles)
        {
            var sourceFilePaths = sourceFiles.Select(file => file.FullName).ToArray();
            var compilerParameters = new CompilerParameters(referencedAssemblies.ToArray())
            {
#if DEBUG
                IncludeDebugInformation = true,
#else
		        IncludeDebugInformation = false,
#endif
                GenerateExecutable = false,
                GenerateInMemory = false,
                WarningLevel = 2,
                CompilerOptions = string.Format($"/optimize /lib:{Path.Combine(".", "lib")}")
            };
            compilerParameters.ReferencedAssemblies.Remove(outputFile.Name);
            compilerParameters.OutputAssembly = outputFile.FullName;

            var compilerResults = compiler.CompileAssemblyFromFile(compilerParameters, sourceFilePaths);
            lastCompilationErrors = compilerResults.Errors;
            GC.Collect();
            return compilerResults.CompiledAssembly;
        }

        public Assembly CompileFromSource(string code)
        {
            var compilerParameters = new CompilerParameters(referencedAssemblies.ToArray())
            {
                GenerateInMemory = true,
                WarningLevel = 2,
                CompilerOptions = string.Format($"/lib:{Path.Combine(".", "lib")}")
            };
            compilerParameters.GenerateInMemory = true;

            var compilerResults = compiler.CompileAssemblyFromSource(compilerParameters, code);
            lastCompilationErrors = compilerResults.Errors;
            if (HasErrors) return null;
            return compilerResults.CompiledAssembly;
        }

        public IEnumerable<string> GetDetailedErrorMessages()
        {
            var errorMessages = new List<string>();
            foreach (CompilerError error in lastCompilationErrors)
            {
                if (error.IsWarning) continue;

                var errorMessage = $"   {error.FileName} Line:{error.Line} Col:{error.Column}";
                if (log.IsErrorEnabled)
                {
                    errorMessage = $"Script compilation failed because: \n{error.ErrorText}\n" + errorMessage;
                }
                errorMessages.Add(errorMessage);
            }
            return errorMessages;
        }

        public IEnumerable<string> GetErrorMessages()
        {
            var errorMessages = new List<string>();
            foreach (CompilerError error in lastCompilationErrors)
            {
                errorMessages.Add(error.ErrorText);
            }
            return errorMessages;
        }
    }
}
#endif
