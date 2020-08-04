#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Protection
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ComponentLoadFailedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.MediaProtectionServiceCompletion Completion
		{
			get
			{
				throw new global::System.NotImplementedException("The member MediaProtectionServiceCompletion ComponentLoadFailedEventArgs.Completion is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Protection.RevocationAndRenewalInformation Information
		{
			get
			{
				throw new global::System.NotImplementedException("The member RevocationAndRenewalInformation ComponentLoadFailedEventArgs.Information is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Protection.ComponentLoadFailedEventArgs.Information.get
		// Forced skipping of method Windows.Media.Protection.ComponentLoadFailedEventArgs.Completion.get
	}
}
