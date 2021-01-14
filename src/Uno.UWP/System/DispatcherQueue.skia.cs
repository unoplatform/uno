using System;
using System.Runtime.CompilerServices;
using Windows.UI.Core;

namespace Windows.System
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
