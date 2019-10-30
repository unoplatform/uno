#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreDispatcher : global::Windows.UI.Core.ICoreAcceleratorKeys
	{
		// Skipping already declared property HasThreadAccess
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Core.CoreDispatcherPriority CurrentPriority
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreDispatcherPriority CoreDispatcher.CurrentPriority is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreDispatcher", "CoreDispatcherPriority CoreDispatcher.CurrentPriority");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreDispatcher.HasThreadAccess.get
		#if __ANDROID__ || __IOS__ || false || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void ProcessEvents( global::Windows.UI.Core.CoreProcessEventsOption options)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreDispatcher", "void CoreDispatcher.ProcessEvents(CoreProcessEventsOption options)");
		}
		#endif
		// Skipping already declared method Windows.UI.Core.CoreDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority, Windows.UI.Core.DispatchedHandler)
		// Skipping already declared method Windows.UI.Core.CoreDispatcher.RunIdleAsync(Windows.UI.Core.IdleDispatchedHandler)
		// Forced skipping of method Windows.UI.Core.CoreDispatcher.AcceleratorKeyActivated.add
		// Forced skipping of method Windows.UI.Core.CoreDispatcher.AcceleratorKeyActivated.remove
		// Forced skipping of method Windows.UI.Core.CoreDispatcher.CurrentPriority.get
		// Forced skipping of method Windows.UI.Core.CoreDispatcher.CurrentPriority.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool ShouldYield()
		{
			throw new global::System.NotImplementedException("The member bool CoreDispatcher.ShouldYield() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool ShouldYield( global::Windows.UI.Core.CoreDispatcherPriority priority)
		{
			throw new global::System.NotImplementedException("The member bool CoreDispatcher.ShouldYield(CoreDispatcherPriority priority) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  void StopProcessEvents()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreDispatcher", "void CoreDispatcher.StopProcessEvents()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryRunAsync( global::Windows.UI.Core.CoreDispatcherPriority priority,  global::Windows.UI.Core.DispatchedHandler agileCallback)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> CoreDispatcher.TryRunAsync(CoreDispatcherPriority priority, DispatchedHandler agileCallback) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryRunIdleAsync( global::Windows.UI.Core.IdleDispatchedHandler agileCallback)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> CoreDispatcher.TryRunIdleAsync(IdleDispatchedHandler agileCallback) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.CoreDispatcher, global::Windows.UI.Core.AcceleratorKeyEventArgs> AcceleratorKeyActivated
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreDispatcher", "event TypedEventHandler<CoreDispatcher, AcceleratorKeyEventArgs> CoreDispatcher.AcceleratorKeyActivated");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreDispatcher", "event TypedEventHandler<CoreDispatcher, AcceleratorKeyEventArgs> CoreDispatcher.AcceleratorKeyActivated");
			}
		}
		#endif
		// Processing: Windows.UI.Core.ICoreAcceleratorKeys
	}
}
