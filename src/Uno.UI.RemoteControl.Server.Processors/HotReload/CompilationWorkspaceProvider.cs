using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System.Linq;
using System.Threading;
using System;
using System.IO;
using System.Reflection;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.Host.HotReload
{
	internal static class CompilationWorkspaceProvider
	{
		private static string MSBuildBasePath;

		public static Task<(Solution, WatchHotReloadService)> CreateWorkspaceAsync(string projectPath, IReporter reporter, CancellationToken cancellationToken)
		{
			var taskCompletionSource = new TaskCompletionSource<(Solution, WatchHotReloadService)>(TaskCreationOptions.RunContinuationsAsynchronously);
			CreateProject(taskCompletionSource, projectPath, reporter, cancellationToken);

			return taskCompletionSource.Task;
		}

		static async void CreateProject(TaskCompletionSource<(Solution, WatchHotReloadService)> taskCompletionSource, string projectPath, IReporter reporter, CancellationToken cancellationToken)
		{
			var workspace = MSBuildWorkspace.Create();

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
			var hotReloadService = new WatchHotReloadService(workspace.Services);
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

			var pi = new System.Diagnostics.ProcessStartInfo(
				"cmd.exe",
				@"/c ""C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe"" -property installationPath"
			)
			{
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			var process = System.Diagnostics.Process.Start(pi);
			process.WaitForExit();
			var installPath = process.StandardOutput.ReadToEnd().Split('\r').First();

			SetupMSBuildLookupPath(installPath);
		}

		private static void SetupMSBuildLookupPath(string installPath)
		{
			Environment.SetEnvironmentVariable("VSINSTALLDIR", installPath);
			Environment.SetEnvironmentVariable("MSBuildSDKsPath", @"C:\Program Files\dotnet\sdk\6.0.100-rc.2.21505.57\Sdks");

			bool MSBuildExists() => File.Exists(Path.Combine(MSBuildBasePath, "Microsoft.Build.dll"));

			MSBuildBasePath = @"C:\Program Files\dotnet\sdk\6.0.100-rc.2.21505.57";

			if (!MSBuildExists())
			{
				MSBuildBasePath = Path.Combine(installPath, "MSBuild\\Current\\Bin");
				if (!MSBuildExists())
				{
					throw new InvalidOperationException($"Invalid Visual studio installation (Cannot find Microsoft.Build.dll)");
				}
			}
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
				var basePath = Path.GetDirectoryName(new Uri(typeof(CompilationWorkspaceProvider).Assembly.CodeBase).LocalPath);

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
						Console.WriteLine($"Selecting first occurrence of assembly [{e.Name}] which can be found at [{duplicates.Select(d => d.CodeBase).JoinBy("; ")}]");
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

							Console.WriteLine($"Loaded [{output.GetName()}] from [{output.CodeBase}]");

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
