using System;
using Windows.UI.Core;

namespace Windows.System
{
	public partial class DispatcherQueue
	{
		[ThreadStatic]
		private static DispatcherQueue _current;

		private DispatcherQueue()
		{
		}

		public DispatcherQueueTimer CreateTimer()
			=> new DispatcherQueueTimer();

		public static DispatcherQueue GetForCurrentThread()
		{
			if (_current == null) // Do not even check for thread access if we already have a value!
			{
#if !__WASM__
				// This check is disabled on WASM until threading support is enabled, since HasThreadAccess is currently user-configured (and defaults to false).
				if (!CoreDispatcher.Main.HasThreadAccess)
				{
					return default;
				}
#endif

				_current = new DispatcherQueue();
			}

			return _current;
		}

		/// <summary>
		/// Enforce access on the UI thread.
		/// </summary>
		internal static void CheckThreadAccess()
		{
#if !__WASM__
			// This check is disabled on WASM until threading support is enabled, since HasThreadAccess is currently user-configured (and defaults to false).
			if (!CoreDispatcher.Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The application called an interface that was marshalled for a different thread.");
			}
#endif
		}

#if !HAS_UNO_WINUI
		public bool TryEnqueue(DispatcherQueueHandler callback)
		{
			return TryEnqueue(DispatcherQueuePriority.Normal, callback);
		}
		public bool TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
		{
			return TryEnqueueNative(priority, callback);
		}
#endif
	}
}
