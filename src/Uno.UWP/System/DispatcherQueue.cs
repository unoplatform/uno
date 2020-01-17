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
			CoreDispatcher.CheckThreadAccess();

			if (_current == null)
			{
				_current = new DispatcherQueue();
			}

			return _current;
		}
	}
}
