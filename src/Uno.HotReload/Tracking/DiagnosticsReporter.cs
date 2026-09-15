using System;
using System.Collections.Immutable;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Uno.HotReload.Tracking;

/// <summary>
/// Centralizes hot-reload diagnostic reporting: formats each diagnostic through
/// <see cref="CSharpDiagnosticFormatter"/>, colors it by severity, routes it to the
/// <see cref="IReporter"/>, and caps how many are formatted per category so a heavily-broken
/// compilation cannot allocate unbounded strings on WASM (memory.grow() is irreversible) — the overflow
/// collapses into a single "… and N more." line. On desktop everything is reported (IDE-like), so the
/// caps are browser-only. A fresh instance is created per hot-reload pass.
/// </summary>
internal sealed class DiagnosticsReporter(IReporter reporter)
{
	// null == unbounded. WASM caps to bound string allocations; desktop reports everything.
	private static readonly int? AnalyzerLimit = OperatingSystem.IsBrowser() ? 5 : null;
	private static readonly int? EmitLimit = OperatingSystem.IsBrowser() ? 5 : null;
	private static readonly int? SolutionUpdateLimit = OperatingSystem.IsBrowser() ? 5 : null;
	private static readonly int? CompilationLimit = OperatingSystem.IsBrowser() ? 20 : null;

	/// <summary>
	/// Source-generator diagnostics — the ones absent from
	/// <see cref="Compilation.GetDiagnostics(System.Threading.CancellationToken)"/> — that were bypassed
	/// so a non-fatal generator error does not block hot reload. Reported as verbose detail.
	/// </summary>
	public void ReportAnalyzerDiagnostics(ImmutableArray<Diagnostic> diagnostics)
		=> Report(reporter.Verbose, diagnostics, AnalyzerLimit);

	/// <summary>EnC emit diagnostics (rude edits and the semantic errors of the types being updated).</summary>
	public void ReportEmitDiagnostics(ImmutableArray<Diagnostic> diagnostics)
		=> Report(reporter.Verbose, diagnostics, EmitLimit);

	/// <summary>Diagnostics produced while updating the solution (typically csproj re-evaluation issues).</summary>
	public void ReportSolutionUpdateDiagnostics(ImmutableArray<Diagnostic> diagnostics)
		=> Report(reporter.Verbose, diagnostics, SolutionUpdateLimit);

	/// <summary>C# compilation errors that block the reload.</summary>
	public void ReportCompilationDiagnostics(ImmutableArray<Diagnostic> diagnostics)
		=> Report(reporter.Output, diagnostics, CompilationLimit);

	private static void Report(Action<string> report, ImmutableArray<Diagnostic> diagnostics, int? limit)
	{
		var reported = 0;
		foreach (var diagnostic in diagnostics)
		{
			if (limit is { } max && reported >= max)
			{
				report($"... and {diagnostics.Length - reported} more.");
				return;
			}

			report(Colorize(diagnostic));
			reported++;
		}
	}

	// ANSI-color the formatted diagnostic by severity, resetting so the color never bleeds into later lines.
	private static string Colorize(Diagnostic diagnostic)
	{
		var message = CSharpDiagnosticFormatter.Instance.Format(diagnostic, CultureInfo.InvariantCulture);
		return diagnostic.Severity switch
		{
			DiagnosticSeverity.Error => $"\x1B[31m{message}\x1B[0m",   // red
			DiagnosticSeverity.Warning => $"\x1B[33m{message}\x1B[0m", // yellow
			_ => message,
		};
	}
}
