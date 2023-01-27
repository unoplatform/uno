using System;
using Uno.UI.Dispatching;

#if HAS_UNO_WINUI && IS_UNO_UI_DISPATCHING_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
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
				// This check is disabled on WASM until threading support is enabled, since HasThreadAccess is currently user-configured (and defaults to false).
				if (
#if __WASM__
					CoreDispatcher.IsThreadingSupported &&
#endif
					!CoreDispatcher.Main.HasThreadAccess)
				{
					return default;
				}

				_current = new DispatcherQueue();
			}

			return _current;
		}

		/// <summary>
		/// Enforce access on the UI thread.
		/// </summary>
		internal static void CheckThreadAccess()
		{
			if (!CoreDispatcher.Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The application called an interface that was marshalled for a different thread.");
			}
		}

		public bool TryEnqueue(DispatcherQueueHandler callback)
			=> TryEnqueue(DispatcherQueuePriority.Normal, callback);

		public bool TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
		{
			var p = priority switch
			{
				DispatcherQueuePriority.Normal => CoreDispatcherPriority.Normal,
				DispatcherQueuePriority.High => CoreDispatcherPriority.High,
				DispatcherQueuePriority.Low => CoreDispatcherPriority.Low,
				_ => CoreDispatcherPriority.Normal
			};

			_ = CoreDispatcher.Main.RunAsync(p, () => callback());

			return true;
		}

		public bool HasThreadAccess
			=> CoreDispatcher.Main.HasThreadAccess;
	}
}
