#nullable disable

#if NET6_0_OR_GREATER
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Linq;
using System.Threading;
using System;
using System.IO;
using System.Reflection;
using Uno.Extensions;
using Uno.UI.RemoteControl.Server.Processors.Helpers;
using System.Collections.Generic;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates
{
	internal static class CompilationWorkspaceProvider
	{
		private static string MSBuildBasePath;

		public static Task<(Solution, WatchHotReloadService)> CreateWorkspaceAsync(string projectPath, IReporter reporter, string[] metadataUpdateCapabilities, CancellationToken cancellationToken)
		{
			var taskCompletionSource = new TaskCompletionSource<(Solution, WatchHotReloadService)>(TaskCreationOptions.RunContinuationsAsynchronously);
			CreateProject(taskCompletionSource, projectPath, reporter, metadataUpdateCapabilities, cancellationToken);

			return taskCompletionSource.Task;
		}

		static async void CreateProject(
			TaskCompletionSource<(Solution, WatchHotReloadService)> taskCompletionSource,
			string projectPath,
			IReporter reporter,
			string[] metadataUpdateCapabilities,
			CancellationToken cancellationToken)
		{
			var intermediatePath = Path.Combine(Path.GetDirectoryName(projectPath), "obj", "hr") + Path.DirectorySeparatorChar;

			Directory.CreateDirectory(intermediatePath);

			var globalProperties = new Dictionary<string, string> {
				// Override the output path so custom compilation lists do not override the
				// main compilation caches, which can invalidate incremental compilation.
				{ "IntermediateOutputPath", intermediatePath },

				// Mark this compilation as hot-reload capable, so generators can act accordingly
				{ "IsHotReloadHost", "True" },
			};

			var workspace = MSBuildWorkspace.Create(globalProperties);

			workspace.WorkspaceFailed += (_sender, diag) =>
			{
				if (diag.Diagnostic.Kind == WorkspaceDiagnosticKind.Warning)
				{
					reporter.Verbose($"MSBuildWorkspace warning: {diag.Diagnostic}");
				}
				else
				{
					if (!diag.Diagnostic.ToString().StartsWith("[Failure] Found invalid data while decoding"))
					{
						taskCompletionSource.TrySetException(new InvalidOperationException($"Failed to create MSBuildWorkspace: {diag.Diagnostic}"));
					}
				}
			};

			await workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken);
			var currentSolution = workspace.CurrentSolution;
			var hotReloadService = new WatchHotReloadService(workspace.Services, metadataUpdateCapabilities);
			await hotReloadService.StartSessionAsync(currentSolution, cancellationToken);

			// Read the documents to memory
			await Task.WhenAll(
				currentSolution.Projects.SelectMany(p => p.Documents.Concat(p.AdditionalDocuments)).Select(d => d.GetTextAsync(cancellationToken)));

			// Warm up the compilation. This would help make the deltas for first edit appear much more quickly
			foreach (var project in currentSolution.Projects)
			{
				await project.GetCompilationAsync(cancellationToken);
			}

			taskCompletionSource.TrySetResult((currentSolution, hotReloadService));
		}

		public static void InitializeRoslyn()
		{
			RegisterAssemblyLoader();

			MSBuildBasePath = BuildMSBuildPath();

			Environment.SetEnvironmentVariable("MSBuildSDKsPath", Path.Combine(MSBuildBasePath, "Sdks"));

			var MSBuildExists = File.Exists(Path.Combine(MSBuildBasePath, "Microsoft.Build.dll"));

			if (!MSBuildExists)
			{
				throw new InvalidOperationException($"Invalid dotnet installation installation (Cannot find Microsoft.Build.dll in [{MSBuildBasePath}])");
			}
		}

		private static string BuildMSBuildPath()
		{
			var result = ProcessHelper.RunProcess("dotnet.exe", "--info");

			if (result.exitCode == 0)
			{
				var reader = new StringReader(result.output);

				while (reader.ReadLine() is string line)
				{
					if (line.Contains("Base Path:"))
					{
						return line.Substring(line.IndexOf(':') + 1).Trim();
					}
				}

				throw new InvalidOperationException($"Unable to find dotnet SDK base path in:\n {result.output}");
			}

			throw new InvalidOperationException("Unable to find dotnet SDK base path");
		}

		private static void RegisterAssemblyLoader()
		{
			// Force assembly loader to consider siblings, when running in a separate appdomain.
			ResolveEventHandler localResolve = (s, e) =>
			{
				if (e.Name == "Mono.Runtime")
				{
					// Roslyn 2.0 and later checks for the presence of the Mono runtime
					// through this check.
					return null;
				}

				var assembly = new AssemblyName(e.Name);
				var basePath = Path.GetDirectoryName(new Uri(typeof(CompilationWorkspaceProvider).Assembly.Location).LocalPath);

				Console.WriteLine($"Searching for [{assembly}] from [{basePath}]");

				// Ignore resource assemblies for now, we'll have to adjust this
				// when adding globalization.
				if (assembly.Name.EndsWith(".resources"))
				{
					return null;
				}

				// Lookup for the highest version matching assembly in the current app domain.
				// There may be an existing one that already matches, even though the
				// fusion loader did not find an exact match.
				var loadedAsm = (
									from asm in AppDomain.CurrentDomain.GetAssemblies()
									where asm.GetName().Name == assembly.Name
									orderby asm.GetName().Version descending
									select asm
								).ToArray();

				if (loadedAsm.Length > 1)
				{
					var duplicates = loadedAsm
						.Skip(1)
						.Where(a => a.GetName().Version == loadedAsm[0].GetName().Version)
						.ToArray();

					if (duplicates.Length != 0)
					{
						Console.WriteLine($"Selecting first occurrence of assembly [{e.Name}] which can be found at [{duplicates.Select(d => d.Location).JoinBy("; ")}]");
					}

					return loadedAsm[0];
				}
				else if (loadedAsm.Length == 1)
				{
					return loadedAsm[0];
				}

				Assembly LoadAssembly(string filePath)
				{
					if (File.Exists(filePath))
					{
						try
						{
							var output = Assembly.LoadFrom(filePath);

							Console.WriteLine($"Loaded [{output.GetName()}] from [{output.Location}]");

							return output;
						}
						catch (Exception ex)
						{
							Console.WriteLine($"Failed to load [{assembly}] from [{filePath}]", ex);
							return null;
						}
					}
					else
					{
						return null;
					}
				}

				var paths = new[] {
					Path.Combine(basePath, assembly.Name + ".dll"),
					Path.Combine(MSBuildBasePath, assembly.Name + ".dll"),
				};

				return paths
					.Select(LoadAssembly)
					.Where(p => p != null)
					.FirstOrDefault();
			};

			AppDomain.CurrentDomain.AssemblyResolve += localResolve;
			AppDomain.CurrentDomain.TypeResolve += localResolve;
		}

	}
}
#endif
