using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.HotReload.Tracking;
using Uno.HotReload.Utils;

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
	/// The newest C# major version the embedded compiler accepts as a <c>LangVersion</c>.
	/// </summary>
	// LanguageVersion encodes major * 100 + minor (CSharp14 = 1400), so integer division yields the major.
	internal static int MaxCSharpVersion { get; } = (int)LanguageVersion.LatestMajor.MapSpecifiedToEffectiveVersion() / 100;

	/// <summary>
	/// Emits one warning per C# project whose SDK-default language version is newer than
	/// <see cref="MaxCSharpVersion"/> (spec 061 R5). The workspace loader drops a
	/// <c>/langversion</c> the embedded compiler cannot parse without any diagnostic, so such a
	/// project silently compiles with an older C# and only surfaces as parse errors on the first
	/// edit — XAML-only edits included. Runs on every solution load, so a reload repeats it.
	/// </summary>
	internal static void WarnOnUnsupportedLanguageVersions(Solution solution, IReporter reporter, CancellationToken ct = default)
	{
		try
		{
			foreach (var project in solution.Projects)
			{
				ct.ThrowIfCancellationRequested();

				if (IsCompiledWithOlderCSharpThanItsSdkDefault(project, out var targetFramework, out var sdkDefaultVersion))
				{
					reporter.Warn(
						$"Project '{project.Name}' ({targetFramework}) defaults to C# {sdkDefaultVersion}.0, but the hot-reload compiler "
						+ $"(Roslyn {Version.ToString(2)}) supports up to C# {MaxCSharpVersion}.0: C# {sdkDefaultVersion} syntax will block hot "
						+ $"reload with errors the build does not have, XAML-only edits included. Update Uno.Sdk, or set "
						+ $"<LangVersion>{MaxCSharpVersion}.0</LangVersion> in the project.");
				}
			}
		}
		catch (Exception e) when (e is not OperationCanceledException)
		{
			// Diagnostics-only sweep: it must never take down the workspace load it observes.
			reporter.Warn($"Language version check failed (hot reload continues): {e.GetType().Name}: {e.Message}");
			reporter.Verbose(e.ToString());
		}
	}

	/// <summary>
	/// The line appended to a blocked hot reload of <paramref name="project"/> when its compile errors
	/// may come from a C# version the embedded compiler does not support, or <see langword="null"/>.
	/// </summary>
	internal static string? GetUnsupportedLanguageVersionHint(Project project)
		=> IsCompiledWithOlderCSharpThanItsSdkDefault(project, out _, out var sdkDefaultVersion)
			? $"'{project.Name}' defaults to C# {sdkDefaultVersion}.0, but the hot-reload compiler (Roslyn {Version.ToString(2)}) "
				+ $"supports up to C# {MaxCSharpVersion}.0, so errors on C# {sdkDefaultVersion} syntax above are not build errors. "
				+ $"Update Uno.Sdk, or set <LangVersion>{MaxCSharpVersion}.0</LangVersion> in the project."
			: null;

	private static bool IsCompiledWithOlderCSharpThanItsSdkDefault(Project project, [NotNullWhen(true)] out string? targetFramework, out int sdkDefaultVersion)
	{
		targetFramework = null;
		sdkDefaultVersion = 0;

		// Versions relative to the compiler resolve to an older C# here than in the build whenever the
		// build's compiler is newer. An explicit numbered LangVersion the embedded compiler can parse is
		// honored as-is; one it cannot parse is dropped and also surfaces as Default.
		if (project.ParseOptions is not CSharpParseOptions
			{
				SpecifiedLanguageVersion: LanguageVersion.Default or LanguageVersion.Latest or LanguageVersion.LatestMajor or LanguageVersion.Preview
			})
		{
			return false;
		}

		return project.TryGetTargetFramework(out targetFramework)
			&& TryGetSdkDefaultCSharpVersion(targetFramework, out sdkDefaultVersion)
			&& sdkDefaultVersion > MaxCSharpVersion;
	}

	/// <summary>
	/// The SDK's default C# version for a .NET 5+ target framework: <c>9 + (major - 5)</c>, as
	/// computed by <c>_MaxSupportedLangVersion</c> in the SDK's <c>Microsoft.CSharp.Core.targets</c>
	/// (which also caps it at the SDK compiler's newest version, unknown here).
	/// </summary>
	private static bool TryGetSdkDefaultCSharpVersion(string targetFramework, out int csharpVersion)
	{
		csharpVersion = 0;

		if (!RoslynExtensions.TryParseShortTargetFramework(targetFramework, out var frameworkVersion, out _))
		{
			return false;
		}

		const string NetPrefix = "net";
		var versionEnd = frameworkVersion.IndexOf('.');
		if (versionEnd <= NetPrefix.Length
			|| !int.TryParse(frameworkVersion.AsSpan(NetPrefix.Length, versionEnd - NetPrefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var major)
			|| major < 5)
		{
			return false;
		}

		csharpVersion = 9 + (major - 5);
		return true;
	}

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
