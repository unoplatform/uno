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
		}

		private bool GetHasThreadAccess() => NSThread.IsMain;
	}
}
