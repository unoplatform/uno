using System;
using System.Runtime.CompilerServices;
using Windows.UI.Core;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
{
	partial class DispatcherQueue
	{
		internal static Func<DispatcherQueuePriority, DispatcherQueueHandler, bool> EnqueueNativeOverride;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool TryEnqueueNative(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
		{
			return EnqueueNativeOverride(priority, callback);
		}
	}
}
