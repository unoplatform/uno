using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private static readonly List<EventHandler<object>> _handlers = new List<EventHandler<object>>();
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

	[JSExport]
	private static void FrameCallback()
	{
		var handlers = _handlers.ToList();
		foreach (var handler in handlers)
		{
			handler(null, null);
		}

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
