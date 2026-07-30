using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Roslyn;

/// <summary>
/// The Microsoft.CodeAnalysis loaded in this process — the one the hot-reload workspace
/// compiles with, which is NOT the SDK's csc (the workspace is built against a NuGet-pinned
/// Roslyn, while the projects it loads are evaluated by the machine's SDK). Everything that
/// must stay aligned with that embedded Roslyn (analyzer flavor selection, load-failure
/// reporting) lives here.
/// </summary>
internal static class EmbeddedRoslyn
{
	/// <summary>
	/// Version of the embedded Microsoft.CodeAnalysis assemblies. Read once; the explicit throw
	/// turns a hypothetical unversioned assembly into a diagnosable failure at first use instead
	/// of a bare NullReferenceException.
	/// </summary>
	internal static Version Version { get; } = typeof(Compilation).Assembly.GetName().Version
		?? throw new InvalidOperationException("The embedded Microsoft.CodeAnalysis assembly has no version.");

	/// <summary>
	/// The MSBuild <c>CompilerApiVersion</c> value matching the embedded Roslyn, computed at
	/// runtime so version bumps never desynchronize it. Analyzer packages multi-target Roslyn
	/// through <c>analyzers/dotnet/roslyn{X.Y}</c> folders, and the SDK selects the flavor for
	/// its own csc — flavors newer than the embedded Roslyn then fail to type-load, and
	/// <see cref="AnalyzerFileReference.GetGenerators(string)"/> silently returns zero
	/// generators (missing generated code in every compile of the affected projects). Passing
	/// this value as a workspace GLOBAL property makes it immutable for the evaluation, so the
	/// SDK's own unconditional assignment is ignored and the selected flavors are loadable.
	/// </summary>
	internal static string CompilerApiVersion { get; } = $"roslyn{Version.Major}.{Version.Minor}";

	/// <summary>
	/// Forces the materialization of every analyzer reference of <paramref name="solution"/> and
	/// emits one warning per (project, unloadable analyzer): the load failure is otherwise
	/// silent (<see cref="AnalyzerFileReference.GetGenerators(string)"/> swallows it — no
	/// exception, no diagnostic) and only surfaces as a wall of missing-member compilation
	/// errors, long after startup. The forced load is one-time work: Roslyn caches it for the
	/// workspace's lifetime — which is also why this must run during solution load, before
	/// anything else materializes the generators: a later sweep would observe the cached result
	/// and no failure event.
	/// </summary>
	internal static void WarnOnAnalyzerLoadFailures(Solution solution, IReporter reporter, CancellationToken ct = default)
	{
		try
		{
			// Only the AnalyzerLoadFailed event carries the real load/type-load failure, and it only
			// fires while the generators materialize: subscribe before forcing, once per distinct
			// reference (same on-disk analyzer referenced by several projects), keeping the first
			// failure per reference. Concurrent map: the event's contract does not pin the thread
			// it fires on while the loader machinery runs.
			var failures = new ConcurrentDictionary<AnalyzerFileReference, AnalyzerLoadFailureEventArgs>();
			// Distinct() uses AnalyzerFileReference equality (FullPath + AssemblyLoader). Under
			// MSBuildWorkspace there is a single shared loader, so this dedups by path as intended.
			// It is kept instance-based on purpose: `failures` and the warning lookup below key off the
			// same reference instances, so switching to path-based dedup (DistinctBy(FullPath)) would
			// silently drop the warning for any project holding a different instance unless those are
			// re-keyed by path too — not worth it for a shared-loader workspace.
			foreach (var reference in solution.Projects.SelectMany(p => p.AnalyzerReferences).OfType<AnalyzerFileReference>().Distinct())
			{
				ct.ThrowIfCancellationRequested();

				void OnLoadFailed(object? sender, AnalyzerLoadFailureEventArgs args) => failures.TryAdd(reference, args);

				reference.AnalyzerLoadFailed += OnLoadFailed;
				try
				{
					_ = reference.GetGenerators(LanguageNames.CSharp);
				}
				finally
				{
					reference.AnalyzerLoadFailed -= OnLoadFailed;
				}
			}

			if (failures.IsEmpty)
			{
				return;
			}

			// Attribute each failure to every project referencing the analyzer, so the log states
			// which projects hot reload will fail on (their compiles will miss the generated code).
			foreach (var project in solution.Projects)
			{
				foreach (var reference in project.AnalyzerReferences.OfType<AnalyzerFileReference>())
				{
					if (failures.TryGetValue(reference, out var failure))
					{
						reporter.Warn(
							$"Analyzer '{Path.GetFileNameWithoutExtension(reference.FullPath)}'{GetFlavorSegment(reference.FullPath)} "
							+ $"failed to load in the hot-reload workspace (Roslyn {Version.ToString(2)}): its generated code will be MISSING "
							+ $"— hot reload will NOT work for project '{project.Name}' (and any project consuming its generated members). "
							+ $"Failure: [{failure.ErrorCode}] {failure.Message}{(failure.Exception is { } e ? $"\n{e}" : "")}");
					}
				}
			}
		}
		catch (Exception e) when (e is not OperationCanceledException)
		{
			// Diagnostics-only sweep: it must never take down the workspace load it observes. A
			// reference this sweep could not even enumerate will surface through the usual
			// missing-generated-code compilation errors instead.
			reporter.Warn($"Analyzer load-failure detection failed (hot reload continues): {e.Message}");
		}
	}

	/// <summary>
	/// Extracts the <c>roslyn{X.Y}</c> analyzer-flavor segment of an analyzer path when present
	/// (e.g. <c>…/analyzers/dotnet/roslyn5.0/cs/….dll</c>) — it names the flavor the SDK
	/// selected, which is the usual culprit when it is newer than <see cref="Version"/>.
	/// </summary>
	private static string GetFlavorSegment(string fullPath)
	{
		foreach (var segment in fullPath.Split('/', '\\'))
		{
			if (segment.StartsWith("roslyn", StringComparison.OrdinalIgnoreCase)
				&& segment.Length > "roslyn".Length
				&& char.IsAsciiDigit(segment["roslyn".Length]))
			{
				return $" ({segment})";
			}
		}

		return string.Empty;
	}
}
