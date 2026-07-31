#nullable enable

using System;
using System.Collections.Generic;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Snapshots a set of per-frame <c>Rendering</c> handlers into a reused buffer and invokes them,
/// isolating the frame from add/remove during dispatch without allocating a fresh array every
/// frame (this runs on every animation frame while any handler is registered).
/// </summary>
/// <remarks>
/// <para>The snapshot buffer is retained between frames, so it MUST NOT be allowed to root any
/// handler (and, transitively, a collectible-<see cref="System.Runtime.Loader.AssemblyLoadContext"/>
/// object) past its dispatch. <see cref="Dispatch"/> clears the whole buffer in a <c>finally</c>,
/// which covers both a throwing handler and residue from a previous, larger frame (a shrinking
/// handler list must not leave stale delegates rooted beyond the current count). Extracted from
/// the WASM <c>CompositionTarget</c> so the buffer-lifetime behaviour is platform-neutral and
/// unit-testable.</para>
/// <para>A throwing handler must never kill the frame loop: each handler is invoked under its own
/// try/catch (logged at Error, dispatch continues with the remaining handlers) so the exception
/// cannot escape through the platform frame-callback boundary.</para>
/// <para>Reentrant dispatch (a handler synchronously re-entering the frame loop) must not clear
/// the shared buffer underneath the outer invocation: a nested call detects the in-progress
/// dispatch and falls back to a locally allocated snapshot instead of the reused buffer.</para>
/// </remarks>
internal sealed class CompositionTargetFrameDispatcher
{
	// Element type is nullable: cleared slots hold null between frames (see Dispatch), so the
	// buffer must not claim to always contain a non-null handler.
	private EventHandler<object>?[] _snapshot = Array.Empty<EventHandler<object>?>();

	// True while an invocation loop is running. Thread-affine by design (the frame callback and
	// any synchronous re-entry run on the same thread); a nested Dispatch must not reuse — and
	// then clear — the shared buffer the outer loop is still iterating.
	private bool _isDispatching;

	/// <summary>
	/// Test seam: the reused snapshot buffer. Slots MUST be null between frames so no handler is
	/// rooted past its dispatch.
	/// </summary>
	internal IReadOnlyList<EventHandler<object>?> Snapshot => _snapshot;

	/// <summary>
	/// Copies <paramref name="handlers"/> into the reused buffer, invokes each with
	/// <c>(null, null)</c>, then clears the whole buffer — always, even if a handler throws.
	/// A handler exception is logged and swallowed so the remaining handlers still run and the
	/// caller's frame loop stays alive.
	/// </summary>
	/// <param name="handlers">The handlers to dispatch.</param>
	/// <param name="syncRoot">
	/// Optional lock protecting <paramref name="handlers"/> against concurrent mutation: when
	/// provided, the copy-to-buffer phase runs under it. The invocation phase runs OUTSIDE the
	/// lock so user handlers never execute while it is held.
	/// </param>
	public void Dispatch(IReadOnlyList<EventHandler<object>> handlers, object? syncRoot = null)
	{
		// A nested (reentrant) dispatch must leave the shared buffer to the outer loop.
		var useSharedBuffer = !_isDispatching;

		EventHandler<object>?[] buffer;
		int count;
		if (syncRoot is null)
		{
			(buffer, count) = FillBuffer(handlers, useSharedBuffer);
		}
		else
		{
			lock (syncRoot)
			{
				(buffer, count) = FillBuffer(handlers, useSharedBuffer);
			}
		}

		if (useSharedBuffer)
		{
			_isDispatching = true;
		}

		try
		{
			for (var i = 0; i < count; i++)
			{
				try
				{
					// Non-null here: slots [0, count) were just populated from the (non-null)
					// handler list above and are only cleared in the finally, after this loop.
					buffer[i]!(null!, null!);
				}
				catch (Exception error)
				{
					// One throwing Rendering handler must not skip the remaining handlers nor
					// escape through the frame-callback boundary (which would halt the frame
					// loop for the process lifetime).
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("A CompositionTarget.Rendering handler failed.", error);
					}
				}
			}
		}
		finally
		{
			if (useSharedBuffer)
			{
				_isDispatching = false;
			}

			// Never let the buffer root a handler past its dispatch — a handler may be the only
			// thing keeping a collectible-ALC object alive. Clearing the entire buffer (not just
			// the first `count` slots) also drops any residue from a previous, larger frame.
			Array.Clear(buffer);
		}
	}

	private (EventHandler<object>?[] Buffer, int Count) FillBuffer(IReadOnlyList<EventHandler<object>> handlers, bool useSharedBuffer)
	{
		var count = handlers.Count;

		EventHandler<object>?[] buffer;
		if (!useSharedBuffer)
		{
			// Reentrant call: a local, GC-tracked snapshot preserves correctness (the outer loop
			// keeps its own buffer); the allocation only ever happens on this exceptional path.
			buffer = new EventHandler<object>?[count];
		}
		else
		{
			if (_snapshot.Length < count)
			{
				_snapshot = new EventHandler<object>?[count];
			}

			buffer = _snapshot;
		}

		for (var i = 0; i < count; i++)
		{
			buffer[i] = handlers[i];
		}

		return (buffer, count);
	}
}
