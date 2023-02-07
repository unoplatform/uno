#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Accessibility
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScreenReaderPositionChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsReadingText
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ScreenReaderPositionChangedEventArgs.IsReadingText is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ScreenReaderPositionChangedEventArgs.IsReadingText");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect ScreenPositionInRawPixels
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect ScreenReaderPositionChangedEventArgs.ScreenPositionInRawPixels is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Rect%20ScreenReaderPositionChangedEventArgs.ScreenPositionInRawPixels");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Accessibility.ScreenReaderPositionChangedEventArgs.ScreenPositionInRawPixels.get
		// Forced skipping of method Windows.UI.Accessibility.ScreenReaderPositionChangedEventArgs.IsReadingText.get
	}
}
