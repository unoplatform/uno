using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Tracking;

namespace Uno.HotReload.Diffing;

/// <summary>
/// Applies a <see cref="ChangeSet"/> on top of a <see cref="Solution"/>,
/// producing the next solution snapshot consumed by
/// <c>HotReloadManager.ProcessSolutionChanged</c>. Implementations may also
/// short-circuit the cycle by setting <see cref="ApplyResult.IsCompleted"/>
/// to <c>true</c> after calling
/// <see cref="HotReloadOperation.Complete(HotReloadOperationResult, System.Exception?, System.Collections.Immutable.ImmutableArray{Microsoft.CodeAnalysis.Diagnostic}?)"/>.
/// </summary>
public interface IHotReloadChangeApplier
{
	/// <summary>
	/// Mutates <paramref name="solution"/> with the document edits / adds /
	/// removes from <paramref name="changeSet"/> and returns the resulting
	/// snapshot. Wrapping appliers may also react to
	/// <see cref="ChangeSet.EditedProjects"/>.
	/// </summary>
	ValueTask<ApplyResult> ApplyChangesAsync(
		Solution solution,
		ChangeSet changeSet,
		HotReloadOperation hotReload,
		CancellationToken ct);
}
