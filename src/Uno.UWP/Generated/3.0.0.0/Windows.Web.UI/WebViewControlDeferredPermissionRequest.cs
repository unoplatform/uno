#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlDeferredPermissionRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint WebViewControlDeferredPermissionRequest.Id is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20WebViewControlDeferredPermissionRequest.Id");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.UI.WebViewControlPermissionType PermissionType
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebViewControlPermissionType WebViewControlDeferredPermissionRequest.PermissionType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=WebViewControlPermissionType%20WebViewControlDeferredPermissionRequest.PermissionType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri Uri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebViewControlDeferredPermissionRequest.Uri is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Uri%20WebViewControlDeferredPermissionRequest.Uri");
			}
		}
		#endif
		// Forced skipping of method Windows.Web.UI.WebViewControlDeferredPermissionRequest.Id.get
		// Forced skipping of method Windows.Web.UI.WebViewControlDeferredPermissionRequest.Uri.get
		// Forced skipping of method Windows.Web.UI.WebViewControlDeferredPermissionRequest.PermissionType.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Allow()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.WebViewControlDeferredPermissionRequest", "void WebViewControlDeferredPermissionRequest.Allow()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Deny()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.WebViewControlDeferredPermissionRequest", "void WebViewControlDeferredPermissionRequest.Deny()");
		}
		#endif
	}
}
