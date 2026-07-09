using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.MSBuild;
using Uno.HotReload;
using Uno.HotReload.Tracking;

namespace Uno.Roslyn.MSBuild;

public static class CompilationWorkspaceProvider
{
	/// <summary>
	/// The only application-provided MSBuild properties re-applied as global properties on the
	/// hot-reload workspace. Everything else captured at build time is deliberately dropped so
	/// the workspace evaluates the projects the same way a plain command-line build would:
	/// <list type="bullet">
	/// <item><c>Configuration</c> — the one capture that shapes the compiled output (DEBUG
	/// constants, Optimize, hot-reload code-gen gates in the source generators).</item>
	/// <item><c>Platform</c> — companion of <c>Configuration</c>; near-inert with its AnyCPU
	/// default but kept aligned with the application build.</item>
	/// <item><c>Solution*</c> — restore the solution context for projects whose evaluation
	/// references $(SolutionDir)-style anchors (the workspace opens a project, not a solution).</item>
	/// </list>
	/// The target framework is intentionally NOT part of this list: the workspace loads every
	/// flavor of a multi-targeted head, and the caller then restricts the resulting solution to
	/// the flavor matching the target framework the running application reported (the provider
	/// has no knowledge of the runtime target framework). The runtime identifier is not forwarded
	/// either — an application built with RID-suffixed output paths falls back to baseline
	/// re-emission at init (known limitation, relies on deterministic builds).
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

	/// <summary>
	/// Opens the hot-reload <see cref="MSBuildWorkspace"/> for <paramref name="projectPath"/>: the
	/// projects are evaluated with the whitelisted global properties only (see
	/// <see cref="_globalPropertiesAllowList"/>). The workspace owns the MSBuild services and is
	/// disposed by the caller's pipeline. Restricting a multi-targeted head to the running
	/// application's flavor is left to the caller — this provider deliberately exposes the raw,
	/// unfiltered <see cref="MSBuildWorkspace"/>.
	/// </summary>
	/// <param name="projectPath">Full path of the head project the application runs.</param>
	/// <param name="reporter">Reporter used to surface workspace diagnostics.</param>
	/// <param name="properties">The MSBuild properties captured by the application build (only whitelisted entries are re-applied globally).</param>
	/// <param name="ct">Cancellation token.</param>
	public static async Task<MSBuildWorkspace> CreateWorkspaceAsync(
		string projectPath,
		IReporter reporter,
		Dictionary<string, string> properties,
		CancellationToken ct)
	{
		if (properties.TryGetValue("UnoHotReloadDiagnosticsLogPath", out var logPath) && logPath is { Length: > 0 })
		{
			HotReloadEnvironment.EnableLogging(logPath);
		}

		var globalProperties = new Dictionary<string, string>
		{
			// Flag the current build as created for hot reload, which allows for running targets
			// or setting props/items in the context of the hot reload workspace.
			["UnoIsHotReloadHost"] = "True",
		};

		foreach (var property in _globalPropertiesAllowList)
		{
			// An empty value would surface to MSBuild as a defined-but-empty global property,
			// which is not the same as an undefined one — skip those (typical for the Solution*
			// group when the application was built from a project instead of a solution).
			if (properties.TryGetValue(property, out var value) && value is { Length: > 0 })
			{
				globalProperties[property] = value;
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
				// We retry a few times to let the build complete. Dispose the failed workspace first:
				// MSBuildWorkspace hosts MSBuild evaluation nodes (real OS resources), so a workspace
				// left behind on a failed attempt would linger for the server's lifetime.
				workspace?.Dispose();
				workspace = null!;
				await Task.Delay(5_000, ct);
			}
		}

		return workspace;
	}
}
