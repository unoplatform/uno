using System;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		private bool GetHasThreadAccess() => true;

		private bool HasWork =>
			_queues[0].Count +
			_queues[1].Count +
			_queues[2].Count +
			_queues[3].Count > 0;

		internal void ProcessEvents(CoreProcessEventsOption option)
		{
			if (option != CoreProcessEventsOption.ProcessAllIfPresent)
			{
				throw new NotSupportedException(option.ToString());
			}

			while (HasWork)
			{
				DispatchItems();
			}
		}
	}

	internal enum CoreProcessEventsOption
	{
		ProcessOneAndAllPending,
		ProcessOneIfPresent,
		ProcessUntilQuit,
		ProcessAllIfPresent,
	}
}
