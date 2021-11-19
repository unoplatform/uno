using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Dispatching
{
	internal sealed partial class CoreDispatcher
	{
		public static bool HasThreadAccessOverride { get; set; } = true;
		 
		private bool GetHasThreadAccess() => HasThreadAccessOverride;

		internal bool IsQueueEmpty => _queues.All(q => q.Count == 0);

		public void ProcessEvents(CoreProcessEventsOption options)
		{
			switch (options)
			{
				case CoreProcessEventsOption.ProcessAllIfPresent:
					while (!IsQueueEmpty)
					{
						DispatchItems();
					}
					break;
				default:
					throw new NotSupportedException("Option " + options + " not supported. Only ProcessAllIfPresent is supported yet.");
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
