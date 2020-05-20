#if HAS_UNO_WINUI
using System;
using Windows.UI.Core;

namespace Microsoft.System
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
	}
}
#endif
