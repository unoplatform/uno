using System;
using Windows.UI.Core;

namespace Windows.System
{
	partial class DispatcherQueue
	{
		[Uno.NotImplemented]
		bool TryEnqueueNative(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
		{
			throw new NotSupportedException();
		}
	}
}
