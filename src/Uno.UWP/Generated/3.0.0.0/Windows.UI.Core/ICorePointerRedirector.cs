#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ICorePointerRedirector 
	{
		// Forced skipping of method Windows.UI.Core.ICorePointerRedirector.PointerRoutedAway.add
		// Forced skipping of method Windows.UI.Core.ICorePointerRedirector.PointerRoutedAway.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerRedirector.PointerRoutedTo.add
		// Forced skipping of method Windows.UI.Core.ICorePointerRedirector.PointerRoutedTo.remove
		// Forced skipping of method Windows.UI.Core.ICorePointerRedirector.PointerRoutedReleased.add
		// Forced skipping of method Windows.UI.Core.ICorePointerRedirector.PointerRoutedReleased.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.ICorePointerRedirector, global::Windows.UI.Core.PointerEventArgs> PointerRoutedAway;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.ICorePointerRedirector, global::Windows.UI.Core.PointerEventArgs> PointerRoutedReleased;
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		 event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Core.ICorePointerRedirector, global::Windows.UI.Core.PointerEventArgs> PointerRoutedTo;
		#endif
	}
}
