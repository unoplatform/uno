#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EffectiveViewportChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double BringIntoViewDistanceX
		{
			get
			{
				throw new global::System.NotImplementedException("The member double EffectiveViewportChangedEventArgs.BringIntoViewDistanceX is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double BringIntoViewDistanceY
		{
			get
			{
				throw new global::System.NotImplementedException("The member double EffectiveViewportChangedEventArgs.BringIntoViewDistanceY is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect EffectiveViewport
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect EffectiveViewportChangedEventArgs.EffectiveViewport is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect MaxViewport
		{
			get
			{
				throw new global::System.NotImplementedException("The member Rect EffectiveViewportChangedEventArgs.MaxViewport is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.EffectiveViewportChangedEventArgs.EffectiveViewport.get
		// Forced skipping of method Windows.UI.Xaml.EffectiveViewportChangedEventArgs.MaxViewport.get
		// Forced skipping of method Windows.UI.Xaml.EffectiveViewportChangedEventArgs.BringIntoViewDistanceX.get
		// Forced skipping of method Windows.UI.Xaml.EffectiveViewportChangedEventArgs.BringIntoViewDistanceY.get
	}
}
