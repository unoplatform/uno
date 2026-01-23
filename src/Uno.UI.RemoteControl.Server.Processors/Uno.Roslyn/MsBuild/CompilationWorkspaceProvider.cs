using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Uno.HotReload;

namespace Uno.Roslyn.MSBuild;

public static class CompilationWorkspaceProvider
{
	public static async Task<Workspace> CreateWorkspaceAsync(
		string projectPath,
		IReporter reporter,
		Dictionary<string, string> properties,
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

		static bool IsValidProperty(string property)
		{
			if (property.Equals("RuntimeIdentifier", StringComparison.OrdinalIgnoreCase))
			{
				// Don't set the runtime identifier since it propagates to libraries as well
				// which do not build using the RuntimeIdentifier being set. For instance, a head
				// building for `iossimulator` will fail if the RuntimeIdentifier is set globally its
				// dependent projects, causing the HR engine to search for pdb/dlls in
				// the bin/Debug/net9.0/iossimulator/*.dll path instead of its original path.

				return false;
			}

			if (property.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase))
			{
				// Noticeably, don't set the "MSBuildVersion" (Forbidden, will fail workspace).
				return false;
			}

			return true;
		}

		var globalProperties = properties.Where(property => IsValidProperty(property.Key)).ToDictionary();

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

		return workspace;
	}
}
