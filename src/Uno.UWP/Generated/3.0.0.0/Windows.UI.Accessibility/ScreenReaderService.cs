#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Accessibility
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScreenReaderService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Accessibility.ScreenReaderPositionChangedEventArgs CurrentScreenReaderPosition
		{
			get
			{
				throw new global::System.NotImplementedException("The member ScreenReaderPositionChangedEventArgs ScreenReaderService.CurrentScreenReaderPosition is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ScreenReaderService() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Accessibility.ScreenReaderService", "ScreenReaderService.ScreenReaderService()");
		}
		#endif
		// Forced skipping of method Windows.UI.Accessibility.ScreenReaderService.ScreenReaderService()
		// Forced skipping of method Windows.UI.Accessibility.ScreenReaderService.CurrentScreenReaderPosition.get
		// Forced skipping of method Windows.UI.Accessibility.ScreenReaderService.ScreenReaderPositionChanged.add
		// Forced skipping of method Windows.UI.Accessibility.ScreenReaderService.ScreenReaderPositionChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Accessibility.ScreenReaderService, global::Windows.UI.Accessibility.ScreenReaderPositionChangedEventArgs> ScreenReaderPositionChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Accessibility.ScreenReaderService", "event TypedEventHandler<ScreenReaderService, ScreenReaderPositionChangedEventArgs> ScreenReaderService.ScreenReaderPositionChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Accessibility.ScreenReaderService", "event TypedEventHandler<ScreenReaderService, ScreenReaderPositionChangedEventArgs> ScreenReaderService.ScreenReaderPositionChanged");
			}
		}
		#endif
	}
}
