#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.ViewManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ActivationViewSwitcher 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowAsStandaloneAsync( int viewId)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ActivationViewSwitcher.ShowAsStandaloneAsync(int viewId) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ShowAsStandaloneAsync( int viewId,  global::Windows.UI.ViewManagement.ViewSizePreference sizePreference)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ActivationViewSwitcher.ShowAsStandaloneAsync(int viewId, ViewSizePreference sizePreference) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsViewPresentedOnActivationVirtualDesktop( int viewId)
		{
			throw new global::System.NotImplementedException("The member bool ActivationViewSwitcher.IsViewPresentedOnActivationVirtualDesktop(int viewId) is not implemented in Uno.");
		}
		#endif
	}
}
