using System;
using System.Collections.Generic;
using System.Linq;
using CoreAnimation;
using Foundation;

namespace Windows.UI.Xaml.Media
{
	public partial class CompositionTarget
	{
		private static List<EventHandler<object>> _handlers = new List<EventHandler<object>>();
		private static CADisplayLink _displayLink;

		public static event EventHandler<object> Rendering
		{
			add
			{
				_handlers.Add(value);

				if (_displayLink == null)
				{
					_displayLink = CADisplayLink.Create(OnFrame);
					// Default == normal UI updates
					_displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);
					// UITracking == updates during scrolling
					_displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.UITracking);
				}
			}
			remove
			{
				_handlers.Remove(value);

				if (_handlers.Count == 0)
				{
					_displayLink.RemoveFromRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);
					_displayLink.RemoveFromRunLoop(NSRunLoop.Main, NSRunLoopMode.UITracking);
					_displayLink = null;
				}
			}
		}

		private static void OnFrame()
		{
			var handlers = _handlers.ToList();
			foreach (var handler in handlers)
			{
				handler(null, null);
			}

		}
	}
}
