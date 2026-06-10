using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Applies a <see cref="ChangeSet"/> on top of a <see cref="Solution"/>,
/// producing the next solution snapshot consumed by
/// <c>HotReloadManager.ProcessSolutionChanged</c>. Implementations report
/// what they did <em>not</em> consume via
/// <see cref="SolutionUpdateResult.IgnoredChanges"/> and surface fatal
/// conditions via <see cref="SolutionUpdateResult.Diagnostics"/>; they must
/// not call <c>HotReloadOperation.Complete</c> themselves — the manager
/// owns the operation lifecycle.
/// </summary>
public interface ISolutionUpdater
{
	/// <summary>
	/// Mutates <paramref name="solution"/> with the document / project edits
	/// from <paramref name="changeSet"/> and returns the resulting snapshot
	/// alongside whatever the updater chose not to handle. Implementations
	/// build <see cref="SolutionUpdateResult.IgnoredChanges"/> with a
	/// <c>changeSet with { … }</c> expression that <em>only</em> zeroes out
	/// the fields they explicitly consumed, so any future <see cref="ChangeSet"/>
	/// member added to the contract is automatically reported as ignored
	/// until an updater opts in.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Reference-equality contract: when the updater performs no observable
	/// mutation it <strong>must</strong> return the same <see cref="Solution"/>
	/// instance it received in <paramref name="solution"/>. The hot-reload
	/// manager keys its <c>NoChanges</c> fast path off
	/// <c>result.Solution == originalSolution</c>; an updater that touches a
	/// document and reverts (or returns an equivalent-but-distinct snapshot)
	/// must produce a fresh instance only when at least one project / document
	/// state changed, otherwise an unnecessary
	/// <c>EmitSolutionUpdateAsync</c> roundtrip is incurred per cycle.
	/// </para>
	/// </remarks>
	ValueTask<SolutionUpdateResult> UpdateAsync(
		Solution solution,
		ChangeSet changeSet,
		CancellationToken ct);
}
