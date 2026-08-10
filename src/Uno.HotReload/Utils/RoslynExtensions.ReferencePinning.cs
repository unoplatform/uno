using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Utils;

public static partial class RoslynExtensions
{
	/// <summary>
	/// Captures, per project, the file-backed metadata references of <paramref name="solution"/>
	/// keyed by assembly simple name (the file name, which is the assembly's simple name for every
	/// build/NuGet asset). This is the reference set the EnC baseline of each project's module
	/// captures when hot reload starts; <see cref="WithBaselineReferenceIdentities"/> pins later
	/// solutions back onto it. A name mapped to two different files in the same project (an
	/// intentionally multi-version baseline) is excluded: pinning cannot pick a side, and Roslyn
	/// owns that case (ENC1098). The excluded names surface through
	/// <paramref name="multiVersionNames"/> so the caller can log the declined coverage.
	/// </summary>
	internal static ImmutableDictionary<ProjectId, ImmutableDictionary<string, PortableExecutableReference>> SnapshotReferenceIdentities(
		this Solution solution,
		out ImmutableArray<string> multiVersionNames)
	{
		var byProject = ImmutableDictionary.CreateBuilder<ProjectId, ImmutableDictionary<string, PortableExecutableReference>>();
		HashSet<string>? allAmbiguous = null;
		foreach (var project in solution.Projects)
		{
			var byName = new Dictionary<string, PortableExecutableReference>(StringComparer.OrdinalIgnoreCase);
			HashSet<string>? ambiguous = null;
			foreach (var reference in project.MetadataReferences)
			{
				if (reference is not PortableExecutableReference { FilePath: { Length: > 0 } path } peReference
					|| Path.GetFileNameWithoutExtension(path) is not { Length: > 0 } name)
				{
					continue;
				}

				if (!byName.TryAdd(name, peReference) && !PathComparer.PathEquals(byName[name].FilePath, path))
				{
					(ambiguous ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(name);
				}
			}

			if (ambiguous is not null)
			{
				foreach (var name in ambiguous)
				{
					byName.Remove(name);
				}

				(allAmbiguous ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).UnionWith(ambiguous);
			}

			byProject.Add(project.Id, byName.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));
		}

		multiVersionNames = allAmbiguous is null
			? []
			: [.. allAmbiguous.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)];
		return byProject.ToImmutable();
	}

	/// <summary>
	/// Returns <paramref name="solution"/> with every file-backed metadata reference whose assembly
	/// simple name exists in <paramref name="baseline"/> but points to another file replaced by the
	/// baseline reference (duplicates collapsing to a single entry). References whose name the
	/// baseline never knew — a genuinely new assembly — are left untouched, as are projects absent
	/// from the baseline. When nothing needs pinning the solution is returned unchanged (same
	/// reference) and <paramref name="pinned"/> is empty.
	/// </summary>
	/// <remarks>
	/// Roslyn 5.x refuses to emit any EnC update for a project whose compilation references an
	/// assembly at another identity than the one its baseline captured (ENC1099 — a rude edit with
	/// zero deltas), while referencing a brand-new assembly is supported. A package added during
	/// hot reload routinely re-binds part of its transitive closure onto assemblies the application
	/// was already built against (issue #24023), so before the emit the manager pins those back to
	/// the identities the running application actually loaded — which is also what the emitted
	/// delta must bind against at runtime.
	/// </remarks>
	internal static Solution WithBaselineReferenceIdentities(
		this Solution solution,
		ImmutableDictionary<ProjectId, ImmutableDictionary<string, PortableExecutableReference>> baseline,
		out ImmutableArray<PinnedReference> pinned)
	{
		var pins = ImmutableArray.CreateBuilder<PinnedReference>();
		foreach (var projectId in solution.ProjectIds)
		{
			if (!baseline.TryGetValue(projectId, out var baselineByName)
				|| baselineByName.IsEmpty
				|| solution.GetProject(projectId) is not { } project)
			{
				continue;
			}

			var references = project.MetadataReferences;
			List<MetadataReference>? updated = null;
			foreach (var reference in references)
			{
				if (reference is PortableExecutableReference { FilePath: { Length: > 0 } path }
					&& Path.GetFileNameWithoutExtension(path) is { Length: > 0 } name
					&& baselineByName.TryGetValue(name, out var baselineReference)
					&& !PathComparer.PathEquals(path, baselineReference.FilePath))
				{
					updated ??= [.. references];
					updated.Remove(reference);

					// Keep a single occurrence of the baseline reference: the conflicting file may
					// have been added ALONGSIDE the baseline one (a blind closure bind) or have
					// REPLACED it (a re-resolution) — both pin to the same baseline identity.
					if (!updated.Any(r => r is PortableExecutableReference pe && PathComparer.PathEquals(pe.FilePath, baselineReference.FilePath)))
					{
						updated.Add(baselineReference);
					}

					pins.Add(new PinnedReference(project.Name, name, path, baselineReference.FilePath!));
				}
			}

			if (updated is not null)
			{
				solution = solution.WithProjectMetadataReferences(projectId, updated);
			}
		}

		pinned = pins.ToImmutable();
		return solution;
	}
}
