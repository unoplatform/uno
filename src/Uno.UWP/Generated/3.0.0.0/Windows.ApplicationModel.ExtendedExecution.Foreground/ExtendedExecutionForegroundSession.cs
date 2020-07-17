#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.ExtendedExecution.Foreground
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ExtendedExecutionForegroundSession : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member ExtendedExecutionForegroundReason ExtendedExecutionForegroundSession.Reason is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession", "ExtendedExecutionForegroundReason ExtendedExecutionForegroundSession.Reason");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Description
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ExtendedExecutionForegroundSession.Description is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession", "string ExtendedExecutionForegroundSession.Description");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ExtendedExecutionForegroundSession() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession", "ExtendedExecutionForegroundSession.ExtendedExecutionForegroundSession()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession.ExtendedExecutionForegroundSession()
		// Forced skipping of method Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession.Description.get
		// Forced skipping of method Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession.Description.set
		// Forced skipping of method Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession.Revoked.add
		// Forced skipping of method Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession.Revoked.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundResult> RequestExtensionAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ExtendedExecutionForegroundResult> ExtendedExecutionForegroundSession.RequestExtensionAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession.Reason.get
		// Forced skipping of method Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession.Reason.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession", "void ExtendedExecutionForegroundSession.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<object, global::Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundRevokedEventArgs> Revoked
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession", "event TypedEventHandler<object, ExtendedExecutionForegroundRevokedEventArgs> ExtendedExecutionForegroundSession.Revoked");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.ExtendedExecution.Foreground.ExtendedExecutionForegroundSession", "event TypedEventHandler<object, ExtendedExecutionForegroundRevokedEventArgs> ExtendedExecutionForegroundSession.Revoked");
			}
		}
		#endif
		// Processing: System.IDisposable
	}
}
