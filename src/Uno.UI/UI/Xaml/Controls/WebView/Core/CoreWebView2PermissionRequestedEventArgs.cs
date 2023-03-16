#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.Web.WebView2.Core
{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
	public partial class CoreWebView2PermissionRequestedEventArgs
	{
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Microsoft.Web.WebView2.Core.CoreWebView2PermissionState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2PermissionState CoreWebView2PermissionRequestedEventArgs.State is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.Web.WebView2.Core.CoreWebView2PermissionRequestedEventArgs", "CoreWebView2PermissionState CoreWebView2PermissionRequestedEventArgs.State");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool IsUserInitiated
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreWebView2PermissionRequestedEventArgs.IsUserInitiated is not implemented in Uno.");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Microsoft.Web.WebView2.Core.CoreWebView2PermissionKind PermissionKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreWebView2PermissionKind CoreWebView2PermissionRequestedEventArgs.PermissionKind is not implemented in Uno.");
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public string Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member string CoreWebView2PermissionRequestedEventArgs.Uri is not implemented in Uno.");
			}
		}
#endif
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2PermissionRequestedEventArgs.Uri.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2PermissionRequestedEventArgs.PermissionKind.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2PermissionRequestedEventArgs.IsUserInitiated.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2PermissionRequestedEventArgs.State.get
		// Forced skipping of method Microsoft.Web.WebView2.Core.CoreWebView2PermissionRequestedEventArgs.State.set
#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral CoreWebView2PermissionRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
#endif
	}
}
