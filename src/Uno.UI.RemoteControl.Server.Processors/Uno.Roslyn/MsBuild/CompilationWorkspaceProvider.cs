using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Uno.HotReload;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;

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
	/// <remarks>
	/// When a head flavor loads with package references but no .NET framework references — the
	/// missing-targeting-pack signature (spec 049): a workload-manifest-pinned targeting pack
	/// absent from the SDK, satisfied only by a restore-time <c>PackageDownload</c> that the
	/// design-time build never runs — the provider recovers by running <c>dotnet restore</c> on
	/// the head project once and reloading the workspace.
	/// </remarks>
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
		var workspaceDiagnostics = new List<WorkspaceDiagnostic>();
		var openRetries = 3;
		var restoreAttempted = false;

		while (true)
		{
			workspaceDiagnostics.Clear();
			workspace = MSBuildWorkspace.Create(globalProperties);

			workspace.WorkspaceFailed += (_sender, diag) =>
			{
				// In some cases, load failures may be incorrectly reported such as this one:
				// https://github.com/dotnet/roslyn/blob/fd45aeb5fbc97d09d4043cef9c9c5142f7638e5c/src/Workspaces/Core/MSBuild/MSBuild/MSBuildProjectLoader.Worker.cs#L245-L259
				// Since the text may be localized we cannot rely on it, so we never fail the project loading for now.
				// The diagnostics are buffered: when the load leaves a head flavor unresolved they
				// are the only trace of the root cause and get re-emitted as warnings (below).
				workspaceDiagnostics.Add(diag.Diagnostic);
				reporter.Verbose($"MSBuildWorkspace {diag.Diagnostic}");
			};

			try
			{
				await workspace.OpenProjectAsync(projectPath, cancellationToken: ct);
			}
			catch (InvalidOperationException) when (openRetries > 1)
			{
				// When we load the workspace right after the app was started, it happens that it "app build" is not yet completed, preventing us to open the project.
				// We retry a few times to let the build complete. Dispose the failed workspace first:
				// MSBuildWorkspace hosts MSBuild evaluation nodes (real OS resources), so a workspace
				// left behind on a failed attempt would linger for the server's lifetime.
				openRetries--;
				workspace.Dispose();
				await Task.Delay(5_000, ct);
				continue;
			}
			catch
			{
				// Non-retried failure (the final attempt, or any other exception type): dispose the
				// partially-created workspace before surfacing the exception — same lingering MSBuild
				// evaluation-node concern as the retry path above.
				workspace.Dispose();
				throw;
			}

			if (!restoreAttempted
				&& workspace.CurrentSolution.GetHeadFlavorsMissingFrameworkReferences(projectPath) is { Count: > 0 } brokenFlavors)
			{
				// Missing-targeting-pack recovery (spec 049), attempted at most once: restore
				// materializes the PackageDownload-satisfied packs into the NuGet cache, from
				// which the next design-time build resolves them. The workspace is reloaded even
				// when the restore reports a failure — a partial restore may still have
				// materialized what this project needs.
				restoreAttempted = true;
				reporter.Warn(
					$"The hot-reload workspace loaded {Describe(brokenFlavors)} without any .NET framework references " +
					"(missing targeting pack at design time); attempting to recover with 'dotnet restore'.");
				workspace.Dispose();
				await DotnetRestoreRunner.TryRestoreAsync(projectPath, reporter, ct);
				continue;
			}

			break;
		}

		ReportUnresolvedHeadFlavors(workspace.CurrentSolution, projectPath, workspaceDiagnostics, reporter);

		return workspace;
	}

	/// <summary>
	/// Surfaces the degraded-load states the pipeline would otherwise hide: the
	/// missing-targeting-pack signature with its remediation (spec 049 D3), and the buffered
	/// <see cref="MSBuildWorkspace.Diagnostics"/> failures — verbose-only on nominal loads —
	/// re-emitted as warnings when a head flavor could not be resolved (spec 049 D4).
	/// </summary>
	private static void ReportUnresolvedHeadFlavors(
		Solution solution,
		string projectPath,
		List<WorkspaceDiagnostic> workspaceDiagnostics,
		IReporter reporter)
	{
		var headFlavors = solution.Projects
			.Where(p => PathComparer.PathEquals(p.FilePath, projectPath))
			.ToList();

		var brokenFlavors = headFlavors.Where(RoslynExtensions.IsMissingFrameworkReferences).ToList();

		if (brokenFlavors.Count > 0)
		{
			// The SDK root is recoverable from the flavors that did resolve their ref packs
			// from an SDK-installed targeting pack; broken and NuGet-cache-resolved flavors
			// contribute nothing (the remediation stays generic then).
			var sdkRoot = headFlavors
				.Select(p => p.TryGetDotnetRootFromFrameworkReferences(out var root) ? root : null)
				.FirstOrDefault(root => root is not null);

			var remediation = sdkRoot is null
				? "Run 'dotnet workload update' with the SDK the dev-server uses (uno-check can locate and fix it), or restore the project with that SDK, then restart the application."
				: $"Run '{Path.Combine(sdkRoot, "dotnet")} workload update' (or a restore with that SDK), then restart the application.";

			reporter.Warn(
				$"The hot-reload workspace loaded {Describe(brokenFlavors)} without any .NET framework references even after a restore attempt: " +
				"the design-time build could not resolve a targeting pack (typically pinned by a workload manifest but not installed). " +
				$"Hot reload will most likely be blocked by compilation errors for this target framework. {remediation} " +
				"See https://github.com/unoplatform/uno/issues/23780.");
		}

		if (headFlavors.Any(p => !p.TryGetTargetFramework(out _)))
		{
			foreach (var diagnostic in workspaceDiagnostics.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure).Take(10))
			{
				reporter.Warn($"MSBuildWorkspace {diagnostic}");
			}
		}
	}

	private static string Describe(IReadOnlyList<Project> projects)
		=> string.Join(", ", projects.Select(p => $"'{p.Name}'"));
}
