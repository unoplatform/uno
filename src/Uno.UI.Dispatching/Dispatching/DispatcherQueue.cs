using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
			Debug.Assert(
				(int)DispatcherQueuePriority.High == 10 &&
				(int)DispatcherQueuePriority.Normal == 0 &&
				(int)DispatcherQueuePriority.Low == -10 &&
				Enum.GetValues<DispatcherQueuePriority>().Length == 3);
		}

		/// <summary>
		/// Gets the dispatcher queue for the main thread.
		/// </summary>
		internal static DispatcherQueue Main { get; } = new DispatcherQueue();

		public DispatcherQueueTimer CreateTimer()
			=> new DispatcherQueueTimer();

		public static DispatcherQueue GetForCurrentThread()
		{
			if (_current == null) // Do not even check for thread access if we already have a value!
			{
				// This check is disabled on WASM until threading support is enabled, since HasThreadAccess is currently user-configured (and defaults to false).
				if (
#if __WASM__
					NativeDispatcher.IsThreadingSupported &&
#endif
					!NativeDispatcher.Main.HasThreadAccess)
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
			if (!NativeDispatcher.Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The application called an interface that was marshalled for a different thread.");
			}
		}

		public bool TryEnqueue(DispatcherQueueHandler callback)
		{
			NativeDispatcher.Main.Enqueue(Unsafe.As<Action>(callback));

			return true;
		}

		public bool TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
		{
			NativeDispatcher.Main.Enqueue(Unsafe.As<Action>(callback), (NativeDispatcherPriority)(~((int)priority - 11) >> 3));

			return true;
		}

		public bool HasThreadAccess
			=> NativeDispatcher.Main.HasThreadAccess;
	}
}
