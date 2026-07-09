using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Uno.Extensions;
using Uno.UI.RemoteControl.Helpers;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates
{
	internal static class CompilationWorkspaceProvider
	{
		private static string MSBuildBasePath = "";

		/// <summary>
		/// The only application-provided MSBuild properties re-applied as global properties on the
		/// hot-reload workspace. The target framework and runtime identifier are intentionally absent:
		/// the workspace loads every flavor of a multi-targeted head and the running one is selected
		/// afterwards (see <see cref="RoslynTargetFrameworkExtensions.FilterHeadProjectTargetFramework"/>),
		/// instead of relying on the fragile build-time TargetFramework promotion.
		/// </summary>
		private static readonly string[] _globalPropertiesAllowList =
		[
			"Configuration",
			"Platform",
			"SolutionFileName",
			"SolutionDir",
			"SolutionExt",
			"SolutionPath",
			"SolutionName",
		];

		public static async Task<(Workspace, WatchHotReloadService, Solution)> CreateWorkspaceAsync(
			string projectPath,
			IReporter reporter,
			string[] metadataUpdateCapabilities,
			Dictionary<string, string> properties,
			string? runtimeTargetFramework,
			CancellationToken ct)
		{
			if (properties.TryGetValue("UnoHotReloadDiagnosticsLogPath", out var logPath) && logPath is { Length: > 0 })
			{
				// Sets Roslyn's environment variable for troubleshooting HR, see:
				// https://github.com/dotnet/roslyn/blob/fc6e0c25277ff440ca7ded842ac60278ee6c9695/src/Features/Core/Portable/EditAndContinue/EditAndContinueService.cs#L72
				Environment.SetEnvironmentVariable("Microsoft_CodeAnalysis_EditAndContinue_LogDir", logPath);

				// Unconditionally enable binlog generation in msbuild. See https://github.com/dotnet/project-system/blob/4210ce79cfd35154dbd858f056bfb9101f290e69/docs/design-time-builds.md?L61
				Environment.SetEnvironmentVariable("MSBUILDDEBUGENGINE", "1");
				Environment.SetEnvironmentVariable("MSBuildDebugEngine", "1"); // For case-sensitive environments like macOS
				Environment.SetEnvironmentVariable("MSBUILDDEBUGPATH", logPath);
			}

			// Restrict the global properties to the allow-list: everything else captured at build
			// time (target framework, runtime identifier, MSBuild internals, …) is deliberately
			// dropped so the workspace evaluates the head for every TargetFrameworks entry. The
			// running flavor is then selected by FilterHeadProjectTargetFramework below, rather than
			// pinned by the build-time TargetFramework promotion.
			var globalProperties = new Dictionary<string, string>
			{
				// Flag the current build as created for hot reload, which allows for running targets
				// or setting props/items in the context of the hot reload workspace.
				["UnoIsHotReloadHost"] = "True",
			};

			foreach (var name in _globalPropertiesAllowList)
			{
				// An empty value would surface to MSBuild as a defined-but-empty global property,
				// which is not the same as an undefined one — skip those (typical for the Solution*
				// group when the application was built from a project instead of a solution).
				if (properties.TryGetValue(name, out var value) && value is { Length: > 0 })
				{
					globalProperties[name] = value;
				}
			}

			MSBuildWorkspace workspace = null!;
			for (var i = 3; i > 0; i--)
			{
				try
				{
					workspace = MSBuildWorkspace.Create(globalProperties);

					workspace.WorkspaceFailed += (_sender, diag) =>
					{
						// In some cases, load failures may be incorrectly reported such as this one:
						// https://github.com/dotnet/roslyn/blob/fd45aeb5fbc97d09d4043cef9c9c5142f7638e5c/src/Workspaces/Core/MSBuild/MSBuild/MSBuildProjectLoader.Worker.cs#L245-L259
						// Since the text may be localized we cannot rely on it, so we never fail the project loading for now.
						reporter.Verbose($"MSBuildWorkspace {diag.Diagnostic}");
					};

					await workspace.OpenProjectAsync(projectPath, cancellationToken: ct);
					break;
				}
				catch (InvalidOperationException) when (i > 1)
				{
					// When we load the workspace right after the app was started, it happens that it "app build" is not yet completed, preventing us to open the project.
					// We retry a few times to let the build complete.
					await Task.Delay(5_000, ct);
				}
			}
			// Restrict a multi-targeted head to the flavor the running application reported: the
			// workspace loads one project per TargetFrameworks entry when the evaluated TargetFramework
			// is empty, and the non-running flavors would otherwise block hot reload with their
			// compilation errors or fail the initial emit (they were never built).
			var currentSolution = workspace.CurrentSolution.FilterHeadProjectTargetFramework(projectPath, runtimeTargetFramework, reporter);
			var hotReloadService = new WatchHotReloadService(workspace.Services, metadataUpdateCapabilities);
			await hotReloadService.StartSessionAsync(currentSolution, ct);

			// Read the documents to memory
			await Task.WhenAll(currentSolution.Projects.SelectMany(p => p.Documents.Concat(p.AdditionalDocuments)).Select(d => d.GetTextAsync(ct)));

			// Warm up the compilation. This would help make the deltas for first edit appear much more quickly
			foreach (var project in currentSolution.Projects)
			{
				await project.GetCompilationAsync(ct);
			}

			return (workspace, hotReloadService, currentSolution);
		}

		public static void InitializeRoslyn(string? workDir)
		{
			RegisterAssemblyLoader();

			MSBuildBasePath = BuildMSBuildPath(workDir);

			var expectedVersion = GetDotnetVersion(workDir);
			var currentVersion = typeof(object).Assembly.GetName().Version;
			if (expectedVersion.Major != currentVersion?.Major)
			{
				if (typeof(CompilationWorkspaceProvider).Log().IsEnabled(LogLevel.Error))
				{
					typeof(CompilationWorkspaceProvider).Log().LogError($"Unable to start the Remote Control server because the application's TargetFramework version does not match the default runtime. Expected: net{expectedVersion.Major}, Current: net{currentVersion?.Major}. Change the TargetFramework version in your project file to match the expected version.");
				}

				throw new InvalidOperationException($"Project TargetFramework version mismatch. Expected: net{expectedVersion.Major}, Current: net{currentVersion?.Major}");
			}

			Environment.SetEnvironmentVariable("MSBuildSDKsPath", Path.Combine(MSBuildBasePath, "Sdks"));

			var MSBuildExists = File.Exists(Path.Combine(MSBuildBasePath, "Microsoft.Build.dll"));

			if (!MSBuildExists)
			{
				throw new InvalidOperationException($"Invalid dotnet installation installation (Cannot find Microsoft.Build.dll in [{MSBuildBasePath}])");
			}
		}

		private static Version GetDotnetVersion(string? workDir)
		{
			var result = ProcessHelper.RunProcess("dotnet.exe", "--version", workDir);

			if (result.exitCode == 0)
			{
				var reader = new StringReader(result.output);

				if (Version.TryParse(reader.ReadLine()?.Split('-').FirstOrDefault(), out var version))
				{
					return version;
				}
			}

			throw new InvalidOperationException("Failed to read dotnet version");
		}

		private static string BuildMSBuildPath(string? workDir)
		{
			var result = ProcessHelper.RunProcess("dotnet.exe", "--info", workDir);

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

		public static void RegisterAssemblyLoader()
		{
			// Force assembly loader to consider siblings, when running in a separate appdomain / ALC.
			Assembly? Load(string name)
			{
				if (name == "Mono.Runtime")
				{
					// Roslyn 2.0 and later checks for the presence of the Mono runtime
					// through this check.
					return null;
				}

				var assembly = new AssemblyName(name);
				var basePath = Path.GetDirectoryName(new Uri(typeof(CompilationWorkspaceProvider).Assembly.Location).LocalPath) ?? "";

				Console.WriteLine($"Searching for [{assembly}] from [{basePath}]");

				// Ignore resource assemblies for now, we'll have to adjust this
				// when adding globalization.
				if (assembly.Name is not null && assembly.Name.EndsWith(".resources", StringComparison.Ordinal))
				{
					return null;
				}

				Assembly? LoadAssembly(string filePath)
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
			}

			AppDomain.CurrentDomain.AssemblyResolve += (snd, e) => Load(e.Name);
			AppDomain.CurrentDomain.TypeResolve += (snd, e) => Load(e.Name);

			// Processors are loaded in a separate ALC in `RemoteControlServer`, which requires
			// to load the files in same ALC to avoid invalid cross-ALC references.
			if (AssemblyLoadContext.GetLoadContext(typeof(CompilationWorkspaceProvider).Assembly) is { } alc)
			{
				alc.Resolving += (snd, e) => Load(e.FullName);
			}
			else
			{
				throw new InvalidOperationException($"Unable to determine the ALC for {nameof(CompilationWorkspaceProvider)}");
			}
		}

	}
}
