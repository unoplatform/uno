#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICoreApplicationUnhandledError 
	{
		// Forced skipping of method Windows.ApplicationModel.Core.ICoreApplicationUnhandledError.UnhandledErrorDetected.add
		// Forced skipping of method Windows.ApplicationModel.Core.ICoreApplicationUnhandledError.UnhandledErrorDetected.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::System.EventHandler<global::Windows.ApplicationModel.Core.UnhandledErrorDetectedEventArgs> UnhandledErrorDetected;
		#endif
	}
}
