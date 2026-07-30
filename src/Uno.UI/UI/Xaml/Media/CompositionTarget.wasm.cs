using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Loader;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private static readonly List<EventHandler<object>> _handlers = new List<EventHandler<object>>();
	private static readonly CompositionTargetFrameDispatcher _frameDispatcher = new();
	private static bool _requestedFrame;

	public static event EventHandler<object> Rendering
	{
		add
		{
			_handlers.Add(value);
			if (!_requestedFrame)
			{
				_requestedFrame = true;
				RequestFrame();
			}
		}
		remove
		{
			_handlers.Remove(value);
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

	private static void ClearAlcHandlersCore(AssemblyLoadContext scope)
	{
		for (var i = _handlers.Count - 1; i >= 0; i--)
		{
			var handler = _handlers[i];

			// The delegate's Target and its Method.DeclaringType are INDEPENDENT ownership sources:
			// a closed delegate can pair a default-ALC target with a collectible-ALC method (or the
			// reverse), so both must be checked. Coalescing them (target ?? method) would let a
			// collectible-ALC method survive behind a non-null default-ALC target. A null assembly
			// contributes false (an open static handler with no declaring type pins nothing).
			var targetAssembly = handler.Target?.GetType().Assembly;
			var methodAssembly = handler.Method.DeclaringType?.Assembly;

			if (IsOwnedByAlcScope(targetAssembly, scope) || IsOwnedByAlcScope(methodAssembly, scope))
			{
				_handlers.RemoveAt(i);
			}
		}

		// scope == null keeps the historical all-non-default semantics (global teardown);
		// otherwise only handlers owned by the specified dying ALC are removed.
		static bool IsOwnedByAlcScope(global::System.Reflection.Assembly assembly, AssemblyLoadContext scope)
		{
			if (assembly is null)
			{
				return false;
			}

			var alc = AssemblyLoadContext.GetLoadContext(assembly);
			if (scope is not null)
			{
				return alc == scope;
			}

			return alc is not null && alc != AssemblyLoadContext.Default;
		}
	}

	[JSExport]
	private static void FrameCallback()
	{
		// The dispatcher snapshots into a reused buffer and always clears it afterwards, so no
		// handler (and thus no collectible-ALC object it may root) lingers in a static buffer past
		// its dispatch — even if a handler throws or the handler list shrank since the last frame.
		_frameDispatcher.Dispatch(_handlers);

		if (_handlers.Count > 0)
		{
			RequestFrame();
		}
		else
		{
			_requestedFrame = false;
		}
	}

	[JSImport($"globalThis.Microsoft.UI.Xaml.Media.{nameof(CompositionTarget)}.requestFrame")]
	internal static partial void RequestFrame();
}
