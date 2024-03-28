using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;

namespace Windows.UI.Xaml.Media
{
	public partial class CompositionTarget
	{
		private static List<EventHandler<object>> _handlers = new List<EventHandler<object>>();
		private static FrameCallback _callback;

		public static event EventHandler<object> Rendering
		{
			add
			{
				_handlers.Add(value);

				if (_callback == null)
				{
					_callback = new FrameCallback();
					Choreographer.Instance.PostFrameCallback(_callback);
				}
			}
			remove
			{
				_handlers.Remove(value);

				if (_handlers.Count == 0)
				{
					Choreographer.Instance.RemoveFrameCallback(_callback);
					_callback = null;
				}
			}
		}

		internal class FrameCallback : Java.Lang.Object, Choreographer.IFrameCallback
		{
			public void DoFrame(long frameTimeNanos) => OnFrame();
		}

		private static void OnFrame()
		{
			var handlers = _handlers.ToList();
			foreach (var handler in handlers)
			{
				handler(null, null);
			}

			// If _callback is null it means that all handlers has been removed, no need to requeue callback
			if (_callback is { })
			{
				Choreographer.Instance.PostFrameCallback(_callback);
			}
		}
	}
}
