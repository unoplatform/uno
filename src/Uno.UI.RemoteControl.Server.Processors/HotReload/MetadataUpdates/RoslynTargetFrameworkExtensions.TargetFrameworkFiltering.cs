using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;

public static partial class RoslynTargetFrameworkExtensions
{
	/// <summary>
	/// Platform identifiers that all designate the skia-desktop family: an application
	/// reporting the <c>skia</c> pseudo-platform runs the flavor spelled either
	/// <c>netX.0-desktop</c> or plain <c>netX.0</c> — a head defines only one of the two.
	/// </summary>
	private static readonly string[] _skiaDesktopPlatforms = ["", "desktop", "skia"];

	/// <summary>
	/// Path comparison matching the rest of the hot-reload processor (case-insensitive on
	/// Windows, case-sensitive elsewhere). 6.6 has no shared <c>PathComparer</c> helper, so the
	/// head-project path match uses this directly.
	/// </summary>
	private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

	/// <summary>
	/// Splits a short TFM (or a runtime-reported descriptor using the same shape) into its
	/// framework-version and platform-identifier parts, dropping the platform version:
	/// <c>net10.0-android36.0</c> → (<c>net10.0</c>, <c>android</c>); <c>net10.0</c> →
	/// (<c>net10.0</c>, <c>""</c>).
	/// </summary>
	internal static bool TryParseShortTargetFramework(string targetFramework, out string frameworkVersion, out string platformIdentifier)
	{
		frameworkVersion = string.Empty;
		platformIdentifier = string.Empty;

		if (string.IsNullOrWhiteSpace(targetFramework))
		{
			return false;
		}

		var dashIndex = targetFramework.IndexOf('-');
		if (dashIndex < 0)
		{
			frameworkVersion = targetFramework;
			return IsFrameworkVersionSegment(frameworkVersion);
		}

		frameworkVersion = targetFramework[..dashIndex];
		var platform = targetFramework[(dashIndex + 1)..];

		// Drop the trailing platform version (e.g. "android36.0" → "android", "windows10.0.19041.0" → "windows").
		var versionStart = platform.Length;
		while (versionStart > 0 && (char.IsAsciiDigit(platform[versionStart - 1]) || platform[versionStart - 1] == '.'))
		{
			versionStart--;
		}

		platformIdentifier = platform[..versionStart];
		return IsFrameworkVersionSegment(frameworkVersion) && platformIdentifier.Length > 0;
	}

	/// <summary>
	/// Determines whether the TFM of a workspace project flavor corresponds to the runtime
	/// target framework reported by the connected application. The comparison ignores
	/// platform versions (<c>net10.0-ios26.0</c> matches <c>net10.0-ios</c>) and treats the
	/// <c>skia</c> pseudo-platform, <c>desktop</c> and the platform-less spelling as the same
	/// family (see <see cref="_skiaDesktopPlatforms"/>).
	/// </summary>
	internal static bool RuntimeTargetFrameworkMatches(string runtimeTargetFramework, string projectTargetFramework)
	{
		if (string.Equals(runtimeTargetFramework, projectTargetFramework, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!TryParseShortTargetFramework(runtimeTargetFramework, out var runtimeVersion, out var runtimePlatform)
			|| !TryParseShortTargetFramework(projectTargetFramework, out var projectVersion, out var projectPlatform))
		{
			return false;
		}

		if (!string.Equals(runtimeVersion, projectVersion, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (string.Equals(runtimePlatform, projectPlatform, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		return _skiaDesktopPlatforms.Contains(runtimePlatform, StringComparer.OrdinalIgnoreCase)
			&& _skiaDesktopPlatforms.Contains(projectPlatform, StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Restricts a solution loaded from a multi-targeted head project to the single flavor
	/// matching the target framework the running application reported. When the evaluated
	/// <c>TargetFramework</c> of a multi-targeted project is empty, Roslyn's
	/// <c>MSBuildWorkspace</c> loads one project per <c>TargetFrameworks</c> entry; the
	/// flavors for the non-running target frameworks either fail to compile (blocking every
	/// update with their compilation errors) or fail the initial emit because they were never
	/// built (missing <see cref="CompilationOutputInfo.AssemblyPath"/>). This pass removes the
	/// non-matching head flavors and every project only reachable from them.
	/// </summary>
	/// <remarks>
	/// The filter is conservative: when the head project cannot be found, when the runtime
	/// target framework is unknown, or when no flavor matches it, the solution is returned
	/// unchanged and the situation is reported — a degraded-but-functional workspace beats a
	/// wrongly-emptied one. Every path logs the loaded flavors, the target framework the
	/// application reported and the flavors kept, so a mis-targeted workspace stays
	/// diagnosable from the logs alone.
	/// </remarks>
	/// <param name="solution">The solution to filter (typically the freshly-opened workspace solution).</param>
	/// <param name="headProjectPath">Full path of the head project (<c>.csproj</c>) the application runs.</param>
	/// <param name="runtimeTargetFramework">The target framework reported by the running application, e.g. <c>net10.0-android</c> or <c>net10.0-skia</c>.</param>
	/// <param name="reporter">Reporter used to surface the filtering decisions.</param>
	public static Solution FilterHeadProjectTargetFramework(
		this Solution solution,
		string headProjectPath,
		string? runtimeTargetFramework,
		IReporter reporter)
	{
		var headFlavors = solution.Projects
			.Where(p => string.Equals(p.FilePath, headProjectPath, _pathComparison))
			.ToList();

		if (headFlavors.Count == 0)
		{
			reporter.Error($"The application project '{headProjectPath}' was not found in the hot-reload workspace; hot reload will most likely not work.");
			return solution;
		}

		if (headFlavors.Count == 1)
		{
			// Single flavor: either the project is single-targeted or MSBuild already pinned
			// the TargetFramework during evaluation. Nothing to filter, but still trace what
			// was loaded against what the application reported: a workspace pinned to the
			// wrong flavor is otherwise invisible in the logs.
			var resolved = headFlavors[0].TryGetTargetFramework(out var tfm);
			var singleFlavor = resolved ? tfm! : $"<unresolved: {headFlavors[0].Name}>";
			if (resolved && !string.IsNullOrWhiteSpace(runtimeTargetFramework) && !RuntimeTargetFrameworkMatches(runtimeTargetFramework, singleFlavor))
			{
				reporter.Warn(
					$"The hot-reload workspace loaded '{headProjectPath}' for the single target framework '{singleFlavor}', " +
					$"which does not match the application's '{runtimeTargetFramework}'. Hot reload updates will most likely " +
					"not apply to the running application.");
			}
			else
			{
				reporter.Output(
					$"Hot-reload workspace loaded '{headProjectPath}' for the single target framework '{singleFlavor}' " +
					$"(application's '{(string.IsNullOrWhiteSpace(runtimeTargetFramework) ? "<not reported>" : runtimeTargetFramework)}'); nothing to filter.");
			}

			return solution;
		}

		var resolvedFlavors = headFlavors
			.Select(p => (Project: p, TargetFramework: p.TryGetTargetFramework(out var tfm) ? tfm : null))
			.ToList();
		var flavorsDescription = string.Join(", ", resolvedFlavors.Select(f => f.TargetFramework ?? $"<unresolved: {f.Project.Name}>"));

		if (string.IsNullOrWhiteSpace(runtimeTargetFramework))
		{
			reporter.Warn(
				$"The hot-reload workspace loaded '{headProjectPath}' for {headFlavors.Count} target frameworks ({flavorsDescription}) " +
				"and the application did not report its target framework, so the workspace cannot be restricted to the running one. " +
				"Hot reload may be blocked by compilation errors coming from the other target frameworks.");
			return solution;
		}

		var matchedFlavors = resolvedFlavors
			.Where(f => f.TargetFramework is not null && RuntimeTargetFrameworkMatches(runtimeTargetFramework, f.TargetFramework))
			.ToList();

		if (matchedFlavors.Count == 0)
		{
			reporter.Warn(
				$"None of the {headFlavors.Count} target frameworks loaded for '{headProjectPath}' ({flavorsDescription}) matches " +
				$"the application's '{runtimeTargetFramework}'. Keeping all of them; hot reload may be blocked by compilation " +
				"errors coming from the other target frameworks.");
			return solution;
		}

		if (matchedFlavors.Count > 1)
		{
			reporter.Warn(
				$"{matchedFlavors.Count} target frameworks loaded for '{headProjectPath}' match the application's " +
				$"'{runtimeTargetFramework}' ({string.Join(", ", matchedFlavors.Select(f => f.TargetFramework))}); keeping all matches.");
		}

		var graph = solution.GetProjectDependencyGraph();

		var keep = new HashSet<ProjectId>();
		foreach (var flavor in matchedFlavors)
		{
			keep.Add(flavor.Project.Id);
			keep.UnionWith(graph.GetProjectsThatThisProjectTransitivelyDependsOn(flavor.Project.Id));
		}

		// Remove the non-matching head flavors and everything only they pull in — but nothing
		// else, so a solution carrying unrelated projects is left untouched.
		var remove = new HashSet<ProjectId>();
		foreach (var flavor in resolvedFlavors)
		{
			if (keep.Contains(flavor.Project.Id))
			{
				continue;
			}

			remove.Add(flavor.Project.Id);
			remove.UnionWith(graph.GetProjectsThatThisProjectTransitivelyDependsOn(flavor.Project.Id));
		}

		remove.ExceptWith(keep);

		foreach (var projectId in remove)
		{
			solution = solution.RemoveProject(projectId);
		}

		reporter.Output(
			$"Hot-reload workspace restricted to '{string.Join(", ", matchedFlavors.Select(f => f.TargetFramework))}' for the " +
			$"application's '{runtimeTargetFramework}' (loaded target frameworks: {flavorsDescription}; " +
			$"{remove.Count} project(s) from the other target frameworks removed).");

		return solution;
	}
}
