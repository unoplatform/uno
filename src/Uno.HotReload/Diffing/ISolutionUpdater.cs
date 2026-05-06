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
	ValueTask<SolutionUpdateResult> UpdateAsync(
		Solution solution,
		ChangeSet changeSet,
		CancellationToken ct);
}
