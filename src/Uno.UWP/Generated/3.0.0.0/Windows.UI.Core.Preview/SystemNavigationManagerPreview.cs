#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core.Preview
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemNavigationManagerPreview 
	{
		// Forced skipping of method Windows.UI.Core.Preview.SystemNavigationManagerPreview.CloseRequested.add
		// Forced skipping of method Windows.UI.Core.Preview.SystemNavigationManagerPreview.CloseRequested.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Core.Preview.SystemNavigationManagerPreview GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member SystemNavigationManagerPreview SystemNavigationManagerPreview.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::System.EventHandler<global::Windows.UI.Core.Preview.SystemNavigationCloseRequestedPreviewEventArgs> CloseRequested
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.Preview.SystemNavigationManagerPreview", "event EventHandler<SystemNavigationCloseRequestedPreviewEventArgs> SystemNavigationManagerPreview.CloseRequested");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.Preview.SystemNavigationManagerPreview", "event EventHandler<SystemNavigationCloseRequestedPreviewEventArgs> SystemNavigationManagerPreview.CloseRequested");
			}
		}
		#endif
	}
}
