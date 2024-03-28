using Foundation;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		private CoreFoundation.DispatchQueue _mainQueue = CoreFoundation.DispatchQueue.MainQueue;

		partial void EnqueueNative(NativeDispatcherPriority priority)
		{
			_mainQueue.DispatchAsync(DispatchItems);
		}

		private bool GetHasThreadAccess() => NSThread.IsMain;
	}
}
