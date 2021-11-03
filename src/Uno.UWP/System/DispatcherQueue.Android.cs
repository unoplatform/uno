using Windows.UI.Core;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Dispatching
#else
namespace Windows.System
#endif
{
	partial class DispatcherQueue
	{
		bool TryEnqueueNative(DispatcherQueuePriority priority, DispatcherQueueHandler callback)
		{
			var p = priority switch
			{
				DispatcherQueuePriority.Normal => CoreDispatcherPriority.Normal,
				DispatcherQueuePriority.High => CoreDispatcherPriority.High,
				DispatcherQueuePriority.Low => CoreDispatcherPriority.Low,
				_ => CoreDispatcherPriority.Normal
			};

			CoreDispatcher.Main.RunAsync(p, () => callback());

			return true;
		}
	}
}
