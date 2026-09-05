using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Utils;

public static partial class RoslynExtensions
{
	// Source-generator diagnostics (reported via ReportDiagnostic) are NOT part of
	// compilation.GetDiagnostics() or EmitResult.Diagnostics — they are only reachable through the
	// internal Project.GetSourceGeneratorDiagnosticsAsync(). Bound by reflection because the API is
	// internal across the Roslyn lines we embed (4.14 → 5.6). Uno.Roslyn (Studio Live) binds the same
	// API the same way.
	private static readonly MethodInfo? _getSourceGeneratorDiagnosticsAsync =
		typeof(Project).GetMethod(
			"GetSourceGeneratorDiagnosticsAsync",
			BindingFlags.Instance | BindingFlags.NonPublic);

	/// <summary>
	/// The diagnostics reported by the source generators of <paramref name="project"/> — the ones absent
	/// from <see cref="Compilation.GetDiagnostics(CancellationToken)"/>. Returns empty when the internal
	/// Roslyn API is unavailable (so callers degrade to their pre-fix behavior rather than throwing).
	/// </summary>
	internal static async ValueTask<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(
		this Project project,
		CancellationToken ct)
	{
		if (_getSourceGeneratorDiagnosticsAsync is null)
		{
			return [];
		}

		var task = (ValueTask<ImmutableArray<Diagnostic>>)_getSourceGeneratorDiagnosticsAsync.Invoke(project, [ct])!;
		return await task.ConfigureAwait(false);
	}

	/// <summary>
	/// <see cref="WithSuppressedDiagnostics(Solution, IEnumerable{string})"/> keyed off the ids of the
	/// given <paramref name="diagnostics"/>.
	/// </summary>
	internal static Solution WithSuppressedDiagnostics(this Solution solution, IEnumerable<Diagnostic> diagnostics)
		=> solution.WithSuppressedDiagnostics(diagnostics.Select(diagnostic => diagnostic.Id));

	/// <summary>
	/// Returns <paramref name="solution"/> with the given diagnostic <paramref name="ids"/> suppressed
	/// on every project (via <see cref="CompilationOptions.SpecificDiagnosticOptions"/>). Suppressing an
	/// id a project never reports is a no-op, so this is safe to apply solution-wide; when
	/// <paramref name="ids"/> is empty the solution is returned unchanged (same reference), so callers pay
	/// nothing on the common no-suppression path.
	/// </summary>
	internal static Solution WithSuppressedDiagnostics(this Solution solution, IEnumerable<string> ids)
	{
		var distinct = ids.Distinct().ToArray();
		if (distinct.Length == 0)
		{
			return solution;
		}

		var result = solution;
		foreach (var project in solution.Projects)
		{
			if (project.CompilationOptions is not { } options)
			{
				continue;
			}

			var specific = options.SpecificDiagnosticOptions;
			foreach (var id in distinct)
			{
				specific = specific.SetItem(id, ReportDiagnostic.Suppress);
			}

			result = result.WithProjectCompilationOptions(project.Id, options.WithSpecificDiagnosticOptions(specific));
		}

		return result;
	}
}
