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
	/// Removes <see cref="Rendering"/> handlers whose target or declaring method belongs to a
	/// non-default (collectible) <see cref="AssemblyLoadContext"/>. A downstream host that loads
	/// previewed apps into their own collectible AssemblyLoadContexts may leave a subscriber
	/// attached after the app unloads (nothing forces per-app unsubscription on WASM, where
	/// <see cref="AssemblyLoadContext.Unloading"/> is never raised); each such subscriber pins its
	/// ALC through this process-lifetime static list. Called from the ALC cleanup hook.
	/// </summary>
	internal static void ClearNonDefaultAlcHandlers()
	{
		var defaultAlc = AssemblyLoadContext.Default;

		for (var i = _handlers.Count - 1; i >= 0; i--)
		{
			var handler = _handlers[i];

			// Static handlers (Target == null) can still reference a non-default ALC via the
			// method's declaring type, so consult both.
			var handlerAssembly = handler.Target?.GetType().Assembly
				?? handler.Method.DeclaringType?.Assembly;
			if (handlerAssembly is null)
			{
				continue;
			}

			var alc = AssemblyLoadContext.GetLoadContext(handlerAssembly);
			if (alc is not null && alc != defaultAlc)
			{
				_handlers.RemoveAt(i);
			}
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
