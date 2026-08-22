using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Loader;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	// Guarded by _handlersGate: subscriptions normally happen on the UI thread, but the ALC
	// teardown sweep can reach this list from other teardown paths, and List<T> corrupts under
	// concurrent mutation. Handlers are INVOKED outside the lock (see FrameCallback).
	private static readonly List<EventHandler<object>> _handlers = new List<EventHandler<object>>();
	private static readonly object _handlersGate = new();
	private static readonly CompositionTargetFrameDispatcher _frameDispatcher = new();

	// True iff a JS animation-frame callback is currently pending. Maintained so that exactly one
	// callback is in flight while handlers exist: FrameCallback clears it on entry and re-arms it
	// after dispatch when handlers remain, so no failure mode can leave it stuck at true with no
	// callback scheduled (which would permanently prevent a later subscription from restarting
	// the loop).
	private static bool _requestedFrame;

	public static event EventHandler<object> Rendering
	{
		add
		{
			lock (_handlersGate)
			{
				_handlers.Add(value);
				if (!_requestedFrame)
				{
					_requestedFrame = true;
					RequestFrame();
				}
			}
		}
		remove
		{
			lock (_handlersGate)
			{
				_handlers.Remove(value);
			}
		}
	}

	/// <summary>
	/// Removes <see cref="Rendering"/> handlers whose target or declaring method belongs to ANY
	/// non-default (collectible) <see cref="AssemblyLoadContext"/>. A downstream host that loads
	/// previewed apps into their own collectible AssemblyLoadContexts may leave a subscriber
	/// attached after the app unloads (nothing forces per-app unsubscription on WASM, where
	/// <see cref="AssemblyLoadContext.Unloading"/> is never raised); each such subscriber pins its
	/// ALC through this process-lifetime static list. Called from the ALC cleanup hook when no
	/// specific dying ALC is identifiable (global teardown).
	/// </summary>
	internal static void ClearNonDefaultAlcHandlers()
		=> ClearAlcHandlersCore(scope: null);

	/// <summary>
	/// Removes <see cref="Rendering"/> handlers owned by the specified dying
	/// <see cref="AssemblyLoadContext"/> only. Unlike <see cref="ClearNonDefaultAlcHandlers"/>,
	/// subscribers from OTHER live secondary ALCs (sibling previewed apps) survive — removal is
	/// destructive (a dropped handler is never re-subscribed), so a whole-process sweep would
	/// break a live sibling app when only one app is being torn down.
	/// </summary>
	internal static void ClearAlcHandlers(AssemblyLoadContext alc)
		=> ClearAlcHandlersCore(alc);

	// The ownership predicate lives in the platform-neutral CompositionTargetHandlerSweep so it is
	// unit-testable (this file only compiles for WASM).
	private static void ClearAlcHandlersCore(AssemblyLoadContext scope)
	{
		int removed;
		lock (_handlersGate)
		{
			removed = CompositionTargetHandlerSweep.RemoveAlcHandlers(_handlers, scope);
		}

		if (removed > 0 && typeof(CompositionTarget).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(CompositionTarget).Log().Debug($"[ALC-CLEANUP] CompositionTarget.Rendering: removed {removed} handler(s) (scope: {scope?.Name ?? "all non-default ALCs"}).");
		}
	}

	[JSExport]
	private static void FrameCallback()
	{
		// The pending callback just fired: clear the flag FIRST so the invariant "_requestedFrame
		// == a callback is pending" holds even if anything below throws. A handler that
		// synchronously re-enters this callback (or subscribes) re-arms it through the same
		// invariant, at most once.
		lock (_handlersGate)
		{
			_requestedFrame = false;
		}

		try
		{
			// The dispatcher snapshots into a reused buffer (copy phase under the gate so a
			// concurrent add/remove/sweep cannot race the copy) and always clears it afterwards,
			// so no handler (and thus no collectible-ALC object it may root) lingers in a static
			// buffer past its dispatch. Individual handler exceptions are caught and logged inside
			// Dispatch, keeping the remaining handlers (and this loop) running.
			_frameDispatcher.Dispatch(_handlers, _handlersGate);
		}
		finally
		{
			// Keep the loop alive no matter what escaped above: while handlers remain, exactly one
			// next-frame callback must be pending. Without this, a single failure would silently
			// stop CompositionTarget.Rendering for the process lifetime.
			lock (_handlersGate)
			{
				if (_handlers.Count > 0 && !_requestedFrame)
				{
					_requestedFrame = true;
					RequestFrame();
				}
			}
		}
	}

	[JSImport($"globalThis.Microsoft.UI.Xaml.Media.{nameof(CompositionTarget)}.requestFrame")]
	internal static partial void RequestFrame();
}
