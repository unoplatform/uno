#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Snapshots a set of per-frame <c>Rendering</c> handlers into a reused buffer and invokes them,
/// isolating the frame from add/remove during dispatch without allocating a fresh array every
/// frame (this runs on every animation frame while any handler is registered).
/// </summary>
/// <remarks>
/// The snapshot buffer is retained between frames, so it MUST NOT be allowed to root any handler
/// (and, transitively, a collectible-<see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// object) past its dispatch. <see cref="Dispatch"/> clears the whole buffer in a <c>finally</c>,
/// which covers both a handler that throws (leaving the not-yet-dispatched slots populated) and
/// residue from a previous, larger frame (a shrinking handler list must not leave stale delegates
/// rooted beyond the current count). Extracted from the WASM <c>CompositionTarget</c> so the
/// buffer-lifetime behaviour is platform-neutral and unit-testable.
/// </remarks>
internal sealed class CompositionTargetFrameDispatcher
{
	// Element type is nullable: cleared slots hold null between frames (see Dispatch), so the
	// buffer must not claim to always contain a non-null handler.
	private EventHandler<object>?[] _snapshot = Array.Empty<EventHandler<object>?>();

	/// <summary>
	/// Test seam: the reused snapshot buffer. Slots MUST be null between frames so no handler is
	/// rooted past its dispatch.
	/// </summary>
	internal IReadOnlyList<EventHandler<object>?> Snapshot => _snapshot;

	/// <summary>
	/// Copies <paramref name="handlers"/> into the reused buffer, invokes each with
	/// <c>(null, null)</c>, then clears the whole buffer — always, even if a handler throws.
	/// </summary>
	public void Dispatch(IReadOnlyList<EventHandler<object>> handlers)
	{
		var count = handlers.Count;
		if (_snapshot.Length < count)
		{
			_snapshot = new EventHandler<object>?[count];
		}

		for (var i = 0; i < count; i++)
		{
			_snapshot[i] = handlers[i];
		}

		try
		{
			for (var i = 0; i < count; i++)
			{
				// Non-null here: slots [0, count) were just populated from the (non-null) handler
				// list above and are only cleared in the finally, after this loop.
				_snapshot[i]!(null!, null!);
			}
		}
		finally
		{
			// Never let the reused buffer root a handler past its dispatch — a handler may be the
			// only thing keeping a collectible-ALC object alive. Clearing the entire buffer (not
			// just the first `count` slots) also drops any residue from a previous, larger frame,
			// and runs even when a handler throws.
			Array.Clear(_snapshot);
		}
	}
}
