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

			var p = priority switch
			{
				DispatcherQueuePriority.Normal => CoreDispatcherPriority.Normal,
				DispatcherQueuePriority.High => CoreDispatcherPriority.High,
				DispatcherQueuePriority.Low => CoreDispatcherPriority.Low
				_ => CoreDispatcherPriority.Normal
			}

			CoreDispatcher.Main.RunAsync(p, callback);

			return EnqueueNativeOverride(priority, callback);
		}
	}
}
