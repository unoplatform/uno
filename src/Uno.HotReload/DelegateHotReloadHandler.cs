using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.HotReload.Tracking;

namespace Uno.HotReload;

/// <summary>
/// Delegate shape for the legacy "send these deltas for the given files"
/// callback. Retained as a typed entry point for ad-hoc adaptation through
/// <see cref="DelegateHotReloadHandler"/>; new consumers should implement
/// <see cref="IHotReloadHandler"/> directly.
/// </summary>
public delegate ValueTask SendUpdatesAsync(ImmutableHashSet<string> files, ImmutableArray<Update> updates, CancellationToken ct);

/// <summary>
/// Convenience <see cref="IHotReloadHandler"/> that adapts a
/// <see cref="SendUpdatesAsync"/> delegate. Forwards the
/// <c>(Files, Deltas)</c> pair to the delegate and discards the rest of
/// <see cref="HotReloadUpdate"/>. Use when a consumer needs a quick
/// drop-in for the <c>(files, updates)</c> shape and does not care about
/// the full update record (test fixtures, harness scenarios, …).
/// </summary>
public sealed class DelegateHotReloadHandler(SendUpdatesAsync send) : IHotReloadHandler
{
	// The handler is now invoked on every terminal outcome, but the legacy (files, updates)
	// delegate only carries deltas — fire it on Success only so existing consumers are unaffected
	// by no-delta (NoChanges / RudeEdit / Failed) cycles.
	public ValueTask OnHotReloadAsync(HotReloadOperationResult result, HotReloadUpdate update, CancellationToken ct)
		=> result is HotReloadOperationResult.Success
			? send(update.Files, update.Deltas, ct)
			: ValueTask.CompletedTask;
}
