#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Transactions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Uno.UI.SourceGenerators.BindableTypeProviders;

namespace Uno.UI.Tasks.LinkerHintsGenerator
{
	/// <summary>
	/// Task used to generate a linker feature list, based on all features being disabled.
	/// </summary>
	public class LinkerHintGeneratorTask_v0 : Microsoft.Build.Utilities.Task
	{
		private const MessageImportance DefaultLogMessageLevel
#if DEBUG
			= MessageImportance.High;
#else
			= MessageImportance.Low;
#endif

		private List<string> _referencedAssemblies = new List<string>();
		private DefaultAssemblyResolver? _assemblyResolver;

		[Required]
		public string AssemblyPath { get; set; } = "";

		[Required]
		public string CurrentProjectPath { get; set; } = "";

		[Required]
		public string ILLinkerPath { get; set; } = "";

		[Required]
		public string TargetFramework { get; set; } = "";

		[Required]
		public string TargetFrameworkVersion { get; set; } = "";

		[Required]
		public string OutputPath { get; set; } = "";

		[Required]
		public string UnoUIPackageBasePath { get; set; } = "";

		public string UnoRuntimeIdentifier { get; set; } = "";

		[Required]
		public Microsoft.Build.Framework.ITaskItem[]? ReferencePath { get; set; }

		[Output]
		public Microsoft.Build.Framework.ITaskItem[]? OutputFeatures { get; set; }

		public override bool Execute()
		{
			// Debugger.Launch();

			BuildReferences();
			OutputPath = AlignPath(OutputPath);

			var pass1Path = Path.Combine(OutputPath, "pass1");

			var features = BuildLinkerFeaturesList();

			Log.LogMessage(DefaultLogMessageLevel, $"Running linker pass 1");

			RunLinker(pass1Path, features);
			var currrentPassFeatures = BuildResultingFeaturesList(pass1Path);

			int pass = 1;
			do
			{
				pass++;

				var currentPassPath = Path.Combine(OutputPath, $"pass{pass}");
				var currentPassLinkerFeatures = FormatFeaturesForLinker(currrentPassFeatures);

				Log.LogMessage(DefaultLogMessageLevel, $"Running linker pass {pass}");
				Log.LogMessage(DefaultLogMessageLevel, $"Pass features {currentPassLinkerFeatures}");
				RunLinker(currentPassPath, currentPassLinkerFeatures);

				var newFeatures = BuildResultingFeaturesList(currentPassPath);

				var newList = ToOrderedFeatureArray(newFeatures);
				var currentList = ToOrderedFeatureArray(currrentPassFeatures);

				currrentPassFeatures = newFeatures;

				if (newList.SequenceEqual(currentList))
				{
					Log.LogMessage(DefaultLogMessageLevel, $"Found stable features list, copying to output directory");
					Log.LogMessage(DefaultLogMessageLevel, $"Final features {FormatFeaturesForLinker(currrentPassFeatures)}");

					foreach (var file in Directory.GetFiles(currentPassPath, "*.dll"))
					{
						var targetPath = Path.Combine(OutputPath, Path.GetFileName(file));
						File.Copy(file, targetPath, true);
					}

					break;
				}
				else
				{
					Log.LogMessage(DefaultLogMessageLevel, $"Feature list changed, linking again");
				}
			}
			while (true);

			OutputFeatures = currrentPassFeatures
				.Select(f => new TaskItem(f.Key, new Dictionary<string, string> { ["Value"] = f.Value }))
				.ToArray();

			return true;
		}

		private static string FormatFeaturesForLinker(Dictionary<string, string> currrentPassFeatures) =>
						string.Join(" ", currrentPassFeatures.Select(h => $"--feature {h.Key} {h.Value}"));
		private static (string Key, string Value)[] ToOrderedFeatureArray(Dictionary<string, string> features) => features
							.Select(p => (p.Key, p.Value))
							.OrderBy(p => p.Key)
							.ToArray();

		private void RunLinker(string outputPath, string features)
		{
			var linkerPath = Path.Combine(ILLinkerPath, "illink.dll");

			var referencedAssemblies = string.Join(" ", _referencedAssemblies
				// Java interop does not link properly when included in our own
				// set of parameters provided to the linker.
				// As we're skipping unresolved symbols already, and that
				// we do not need a functioning output, we can remove the assembly
				// altogether.
				.Where(r => !r.EndsWith("Java.Interop.dll", StringComparison.Ordinal))
				.Distinct()
				.Select(r => $"-reference \"{r}\" "));

			var parameters = new List<string>()
			{
				$"--feature UnoBindableMetadata false",
				$"--verbose",
				$"--deterministic",
				// $"--used-attrs-only true", // not used to keep additional linker hints
				$"--skip-unresolved true",
				$"-b true",
				$"-a {AssemblyPath} entrypoint",
				$"-out {outputPath}",
				referencedAssemblies,
				features,
			};

			var paramString = string.Join("\n", parameters);
			var file = Path.GetTempFileName();
			File.WriteAllText(file, paramString);

			Directory.CreateDirectory(OutputPath);

			var res = StartProcess("dotnet", $"\"{linkerPath}\" @{file}", CurrentProjectPath);

			if (!string.IsNullOrEmpty(res.error))
			{
				Log.LogError(res.error);
			}
		}

		private Dictionary<string, string> BuildResultingFeaturesList(string resultPath)
		{
			var sourceList = new List<string>();
			sourceList.AddRange(Directory.GetFiles(resultPath, "*.dll"));

			var assemblies = new List<AssemblyDefinition>(
				from asmPath in sourceList.Distinct()
				let asm = ReadAssembly(asmPath)
				where asm != null
				select asm
			);

			var features = new Dictionary<string, string>();

			var originalLinkerHints = FindAvailableLinkerHintsFromOriginalList();

			var availableTypes = BuildAvailableTypes(assemblies);

			foreach (var hint in originalLinkerHints)
			{
				features[hint] = "false";
			}

			foreach (var asm in assemblies)
			{
				foreach (var type in asm.MainModule.Types)
				{
					// Search for dependency object types that are still available after the current
					// linker pass.
					if (IsDependencyObject(type))
					{
						features[LinkerHintsHelpers.GetPropertyAvailableName(type.FullName)] = "true";
					}
				}

				// Search for additional types that may still be available after the current
				// linker pass.
				var additionalLinkerHints = asm
					.MainModule
					.GetCustomAttributes()
					.Where(a => a.AttributeType.FullName == "Uno.Foundation.Diagnostics.CodeAnalysis.AdditionalLinkerHintAttribute");

				foreach (var additionalLinkerHint in additionalLinkerHints)
				{
					if (!additionalLinkerHint.HasConstructorArguments)
					{
						throw new InvalidOperationException($"The AdditionalLinkerHintAttribute must have one ctor parameter");
					}

					if (additionalLinkerHint.ConstructorArguments[0].Value is string typeName)
					{
						if (availableTypes.Contains(typeName))
						{
							features[LinkerHintsHelpers.GetPropertyAvailableName(typeName)] = "true";
						}
					}
				}
			}

			return features;
		}


		private bool IsDependencyObject(TypeDefinition type)
		{
			if (type.Interfaces.Any(c => c.InterfaceType.FullName == "Windows.UI.Xaml.DependencyObject"))
			{
				return true;
			}

			try
			{
				if (type.BaseType != null && IsDependencyObject(type.BaseType.Resolve()))
				{
					return true;
				}
			}
			catch (Exception e)
			{
				Log.LogMessage(DefaultLogMessageLevel, $"Failed to resolve base types for {type.FullName}: {e.Message}");
			}

			return false;
		}

		private string BuildLinkerFeaturesList()
		{
			var assemblySearchList = BuildOriginalResourceSearchList();

			var hints = FindAvailableLinkerHints(assemblySearchList);

			var output = string.Join(" ", hints.Select(h => $"--feature {h} false"));

			assemblySearchList.ForEach(a => a.Dispose());

			return output;
		}

		private List<string> FindAvailableLinkerHintsFromOriginalList()
		{
			var originalList = BuildOriginalResourceSearchList();
			var originalLinkerHints = FindAvailableLinkerHints(originalList);

			originalList.ForEach(a => a.Dispose());

			return originalLinkerHints;
		}

		private static List<string> FindAvailableLinkerHints(List<AssemblyDefinition> assemblySearchList)
		{
			var hints = new List<string>();

			foreach (var asm in assemblySearchList)
			{
				if (asm.MainModule.Types.FirstOrDefault(t => t.Name == "__LinkerHints") is { } linkerHints)
				{
					foreach (var prop in linkerHints.Properties)
					{
						hints.Add(prop.Name);
					}
				}
			}

			return hints.Distinct().ToList();
		}

		private static HashSet<string> BuildAvailableTypes(List<AssemblyDefinition> assemblySearchList)
		{
			HashSet<string> map = new();

			foreach (var asm in assemblySearchList)
			{
				foreach (var type in asm.MainModule.Types)
				{
					if (!map.Contains(type.FullName))
					{
						map.Add(type.FullName);
					}
				}
			}

			return map;
		}

		private List<AssemblyDefinition> BuildOriginalResourceSearchList()
		{
			var sourceList = new List<string>();

			sourceList.AddRange(_referencedAssemblies);

			// Add the main assembly last so it can have a final say
			sourceList.Add(AssemblyPath);

			return new List<AssemblyDefinition>(
				from asmPath in sourceList.Distinct()
				let asm = ReadAssembly(asmPath)
				where asm != null
				select asm
			);
		}

		private AssemblyDefinition? ReadAssembly(string asmPath)
		{
			try
			{
				return AssemblyDefinition.ReadAssembly(asmPath, new ReaderParameters { AssemblyResolver = _assemblyResolver });
			}
			catch (Exception ex)
			{
				Log.LogMessage(MessageImportance.Low, $"Failed to read assembly {ex}");
				return null;
			}
		}

		private void BuildReferences()
		{
			if (ReferencePath != null)
			{
				var unoUIPackageBasePath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(UnoUIPackageBasePath)));

				foreach (var referencePath in ReferencePath)
				{
					var isReferenceAssembly = referencePath.GetMetadata("PathInPackage")?.StartsWith("ref/", StringComparison.OrdinalIgnoreCase) ?? false;
					var hasConcreteAssembly = isReferenceAssembly && ReferencePath.Any(innerReference => HasConcreteAssemblyForReferenceAssembly(innerReference, referencePath));

					var name = Path.GetFileName(referencePath.ItemSpec);
					_referencedAssemblies.Add(RewriteReferencePath(referencePath.ItemSpec, unoUIPackageBasePath, UnoRuntimeIdentifier));
				}

				var searchPaths = ReferencePath
						.Select(p => Path.GetDirectoryName(p.ItemSpec))
						.Distinct()
						.ToArray();

				_assemblyResolver = new DefaultAssemblyResolver();

				foreach (var assembly in searchPaths)
				{
					_assemblyResolver.AddSearchDirectory(assembly);
				}
			}

			string RewriteReferencePath(string referencePath, string unoUIPackageBasePath, string unoRuntimeIdentifier)
			{
				var separator = Path.DirectorySeparatorChar;
				unoRuntimeIdentifier = unoRuntimeIdentifier.ToLowerInvariant();

				var runtimeTargetFramework =
					new Version(TargetFrameworkVersion) >= new Version("8.0")
					? "net8.0"
					: "netstandard2.0";

				var isUnoRuntimeEnabled = (unoRuntimeIdentifier == "skia" || unoRuntimeIdentifier == "webassembly") &&
						referencePath.StartsWith(unoUIPackageBasePath, StringComparison.Ordinal);

				if (isUnoRuntimeEnabled)
				{
					var originalFolderPath = $"lib{separator}{runtimeTargetFramework}";
					var preUno46FolderPart = $"uno-runtime{separator}{unoRuntimeIdentifier}";
					var postUno46FolderPathPart = $"uno-runtime{separator}{runtimeTargetFramework}{separator}{unoRuntimeIdentifier}";

					var post46Path = referencePath.Replace(originalFolderPath, postUno46FolderPathPart);
					var pre46Path = referencePath.Replace(originalFolderPath, preUno46FolderPart);

					if (File.Exists(post46Path))
					{
						return post46Path;
					}
					else if (File.Exists(pre46Path))
					{
						return pre46Path;
					}
				}

				return referencePath;
			}
		}

		private static bool HasConcreteAssemblyForReferenceAssembly(ITaskItem other, ITaskItem referenceAssembly)
			=> Path.GetFileName(other.ItemSpec) == Path.GetFileName(referenceAssembly.ItemSpec) && (other.GetMetadata("PathInPackage")?.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) ?? false);


		private string AlignPath(string outputPath)
			=> outputPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Replace(new string(Path.DirectorySeparatorChar, 2), Path.DirectorySeparatorChar.ToString());

		private (int exitCode, string output, string error) StartProcess(string executable, string parameters, string workingDirectory)
		{
			Log.LogMessage(
				DefaultLogMessageLevel,
				$"[{workingDirectory}] {executable} {parameters}");

			var p = new Process
			{
				StartInfo =
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					FileName = executable,
					Arguments = parameters
				}
			};

			if (workingDirectory != null)
			{
				p.StartInfo.WorkingDirectory = workingDirectory;
			}

			var output = new StringBuilder();
			var error = new StringBuilder();
			var elapsed = Stopwatch.StartNew();

			p.OutputDataReceived += (s, e) => { if (e.Data != null) { Log.LogMessage(DefaultLogMessageLevel, $"[{elapsed.Elapsed}] {e.Data}"); output.Append(e.Data); } };
			p.ErrorDataReceived += (s, e) => { if (e.Data != null) { Log.LogError($"[{elapsed.Elapsed}] {e.Data}"); error.Append(e.Data); } };

			if (p.Start())
			{
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
				var exitCore = p.ExitCode;
				p.CancelErrorRead();
				p.CancelOutputRead();
				p.Close();

				return (exitCore, output.ToString(), error.ToString());
			}
			else
			{
				throw new Exception($"Failed to start [{executable}]");
			}

		}
	}
}
