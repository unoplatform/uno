#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class DispatcherQueue 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasThreadAccess
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DispatcherQueue.HasThreadAccess is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.System.DispatcherQueue.CreateTimer()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TryEnqueue( global::Windows.System.DispatcherQueueHandler callback)
		{
			throw new global::System.NotImplementedException("The member bool DispatcherQueue.TryEnqueue(DispatcherQueueHandler callback) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool TryEnqueue( global::Windows.System.DispatcherQueuePriority priority,  global::Windows.System.DispatcherQueueHandler callback)
		{
			throw new global::System.NotImplementedException("The member bool DispatcherQueue.TryEnqueue(DispatcherQueuePriority priority, DispatcherQueueHandler callback) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownStarting.add
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownStarting.remove
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownCompleted.add
		// Forced skipping of method Windows.System.DispatcherQueue.ShutdownCompleted.remove
		// Forced skipping of method Windows.System.DispatcherQueue.HasThreadAccess.get
		// Skipping already declared method Windows.System.DispatcherQueue.GetForCurrentThread()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.DispatcherQueue, object> ShutdownCompleted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, object> DispatcherQueue.ShutdownCompleted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, object> DispatcherQueue.ShutdownCompleted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.DispatcherQueue, global::Windows.System.DispatcherQueueShutdownStartingEventArgs> ShutdownStarting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, DispatcherQueueShutdownStartingEventArgs> DispatcherQueue.ShutdownStarting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.DispatcherQueue", "event TypedEventHandler<DispatcherQueue, DispatcherQueueShutdownStartingEventArgs> DispatcherQueue.ShutdownStarting");
			}
		}
		#endif
	}
}
