using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Outcome of an <see cref="ISolutionUpdater.UpdateAsync"/> call.
/// </summary>
/// <param name="Solution">
/// The solution snapshot the manager should keep working with. May be the
/// original solution unchanged when the updater could not (or chose not to)
/// produce a new snapshot.
/// </param>
/// <param name="IgnoredChanges">
/// The subset of the input <see cref="ChangeSet"/> the updater did not
/// consume. Implementations should construct this with a record-<c>with</c>
/// expression that <em>only</em> zeroes out the fields they explicitly
/// handled; anything else (existing or future fields) flows through as
/// ignored automatically. The manager surfaces these via
/// <see cref="Tracking.HotReloadOperation.NotifyIgnored(string)"/>.
/// </param>
/// <param name="Diagnostics">
/// Diagnostics produced while computing the update — typically csproj
/// re-evaluation errors or other "we cannot proceed safely" signals.
/// When this contains any <see cref="DiagnosticSeverity.Error"/>, the
/// manager short-circuits the cycle with
/// <see cref="Tracking.HotReloadOperationResult.Failed"/>; updater
/// implementations never call <c>HotReloadOperation.Complete</c> themselves.
/// </param>
public sealed record SolutionUpdateResult(
	Solution Solution,
	ChangeSet IgnoredChanges,
	ImmutableArray<Diagnostic> Diagnostics)
{
	/// <summary>
	/// Convenience constructor for the success path: no diagnostics emitted.
	/// </summary>
	public SolutionUpdateResult(Solution solution, ChangeSet ignoredChanges)
		: this(solution, ignoredChanges, []) { }
}
