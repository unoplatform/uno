using System;
using Foundation;
using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		private CoreFoundation.DispatchQueue _mainQueue = CoreFoundation.DispatchQueue.MainQueue;
		private bool _queued;

		partial void EnqueueNative(NativeDispatcherPriority priority)
		{
			if (!_queued)
			{
				_queued = true;
				_mainQueue.DispatchAsync(NativeDispatchItems);
			}
		}

		private void NativeDispatchItems()
		{
			_queued = false;

			DispatchItems();

			if (IsRendering)
			{
				// As we're in continuous rendering mode, we need to re-enqueue
				// the rendering as long as we render on the ui thread.

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Dispatch next rendering");
				}

				EnqueueNative(NativeDispatcherPriority.Normal);
			}
		}

		private bool GetHasThreadAccess() => NSThread.IsMain;
	}
}
