using System;
using Windows.UI.Core;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
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
