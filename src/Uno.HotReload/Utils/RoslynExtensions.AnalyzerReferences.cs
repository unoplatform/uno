using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Uno.HotReload.Roslyn;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Utils;

public static partial class RoslynExtensions
{
	/// <summary>
	/// Replaces every <see cref="AnalyzerFileReference"/> of <paramref name="solution"/> with one
	/// backed by a <see cref="CollectibleAnalyzerAssemblyLoader"/>, so analyzers load in
	/// COLLECTIBLE contexts compatible with the collectible per-application context hosting the
	/// embedded Roslyn. Roslyn 5.x's own loader creates non-collectible analyzer contexts, and
	/// the runtime forbids referencing a collectible assembly from a non-collectible one — every
	/// analyzer load would fail (silently: zero generators) — see
	/// <see cref="CollectibleAnalyzerAssemblyLoader"/>. Pure snapshot transform: the underlying
	/// workspace (and the .csproj files an applied change would rewrite) is never touched.
	/// </summary>
	internal static Solution WithCollectibleAnalyzerReferences(this Solution solution, IReporter? reporter = null)
	{
		// One loader (and one set of per-directory contexts) per solution snapshot lineage: every
		// project sharing an analyzer path shares its loaded assembly, mirroring Roslyn's caching.
		var loader = new CollectibleAnalyzerAssemblyLoader(reporter);

		foreach (var projectId in solution.ProjectIds)
		{
			var references = solution.GetProject(projectId)!.AnalyzerReferences;
			if (references.OfType<AnalyzerFileReference>().Any())
			{
				solution = solution.WithProjectAnalyzerReferences(
					projectId,
					references.Select(r => r is AnalyzerFileReference file ? new AnalyzerFileReference(file.FullPath, loader) : r));
			}
		}

		return solution;
	}
}
