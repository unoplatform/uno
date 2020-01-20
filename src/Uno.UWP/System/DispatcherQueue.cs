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
				if (!CoreDispatcher.Main.HasThreadAccess)
				{
					return default;
				}

				_current = new DispatcherQueue();
			}

			return _current;
		}
	}
}
