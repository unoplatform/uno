using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload.Tracking;

namespace Uno.HotReload;

/// <summary>
/// Consumer end of the hot-reload pipeline. Invoked by <see cref="HotReloadManager"/> on
/// <b>every</b> terminal cycle outcome — with the computed <see cref="HotReloadOperationResult"/>
/// and the full <see cref="HotReloadUpdate"/> — so a handler can react per-outcome, including
/// performing delta-independent side-effects (e.g. staging resolved package assemblies) on a
/// no-delta cycle that mutated the solution but emitted no IL/metadata delta.
/// </summary>
/// <remarks>
/// Implementations may chain (decorator pattern): a wrapping handler can inspect the update,
/// perform side-effects, then delegate to an inner handler for the actual delta transport.
/// </remarks>
public interface IHotReloadHandler
{
	/// <summary>
	/// Process a completed hot-reload cycle. Called for each terminal outcome —
	/// <see cref="HotReloadOperationResult.Success"/>, <see cref="HotReloadOperationResult.NoChanges"/>,
	/// <see cref="HotReloadOperationResult.RudeEdit"/> and <see cref="HotReloadOperationResult.Failed"/>.
	/// <paramref name="update"/>'s <see cref="HotReloadUpdate.Deltas"/> is non-empty only on
	/// <see cref="HotReloadOperationResult.Success"/>; its <see cref="HotReloadUpdate.SolutionUpdate"/>
	/// is populated on every path reached after the solution update (so a custom
	/// <c>SolutionUpdateResult</c> subtype's payload is visible on no-delta cycles too).
	/// </summary>
	/// <remarks>
	/// Handlers that only act on a successful delta should guard on <paramref name="result"/> at the
	/// top and return early. A thrown exception (other than <see cref="System.OperationCanceledException"/>,
	/// which propagates) completes the operation as <see cref="HotReloadOperationResult.Failed"/> — the
	/// reload did not take effect on the consumer side.
	/// </remarks>
	ValueTask OnHotReloadAsync(HotReloadOperationResult result, HotReloadUpdate update, CancellationToken ct);
}
