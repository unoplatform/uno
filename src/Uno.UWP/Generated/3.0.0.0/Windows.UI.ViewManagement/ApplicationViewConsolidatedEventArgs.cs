#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ApplicationViewConsolidatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUserInitiated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ApplicationViewConsolidatedEventArgs.IsUserInitiated is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsAppInitiated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ApplicationViewConsolidatedEventArgs.IsAppInitiated is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.ViewManagement.ApplicationViewConsolidatedEventArgs.IsUserInitiated.get
		// Forced skipping of method Windows.UI.ViewManagement.ApplicationViewConsolidatedEventArgs.IsAppInitiated.get
	}
}
