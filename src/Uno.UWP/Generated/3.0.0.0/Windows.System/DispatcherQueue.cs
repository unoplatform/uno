#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DispatcherQueue 
	{
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.System.DispatcherQueueTimer CreateTimer()
		{
			throw new global::System.NotImplementedException("The member DispatcherQueueTimer DispatcherQueue.CreateTimer() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryEnqueue( global::Windows.System.DispatcherQueueHandler callback)
		{
			throw new global::System.NotImplementedException("The member bool DispatcherQueue.TryEnqueue(DispatcherQueueHandler callback) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool TryEnqueue( global::Windows.System.DispatcherQueuePriority priority,  global::Windows.System.DispatcherQueueHandler callback)
		{
			throw new global::System.NotImplementedException("The member bool DispatcherQueue.TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownStarting.add
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownStarting.remove
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownCompleted.add
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownCompleted.remove
		#if false
		[global::Uno.NotImplemented]
		public static global::Windows.System.DispatcherQueue GetForCurrentThread()
		{
			throw new global::System.NotImplementedException("The member DispatcherQueue DispatcherQueue.GetForCurrentThread() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.DispatcherQueue, object> ShutdownCompleted
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, object> DispatcherQueue.ShutdownCompleted");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, object> DispatcherQueue.ShutdownCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.DispatcherQueue, global::Windows.System.DispatcherQueueShutdownStartingEventArgs> ShutdownStarting
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, DispatcherQueueShutdownStartingEventArgs> DispatcherQueue.ShutdownStarting");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, DispatcherQueueShutdownStartingEventArgs> DispatcherQueue.ShutdownStarting");
			}
		}
		#endif
	}
}
