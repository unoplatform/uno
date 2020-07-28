#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Web.UI.Interop
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebViewControlProcessOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Web.UI.Interop.WebViewControlProcessCapabilityState PrivateNetworkClientServerCapability
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebViewControlProcessCapabilityState WebViewControlProcessOptions.PrivateNetworkClientServerCapability is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.Interop.WebViewControlProcessOptions", "WebViewControlProcessCapabilityState WebViewControlProcessOptions.PrivateNetworkClientServerCapability");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string EnterpriseId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebViewControlProcessOptions.EnterpriseId is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.Interop.WebViewControlProcessOptions", "string WebViewControlProcessOptions.EnterpriseId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebViewControlProcessOptions() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Web.UI.Interop.WebViewControlProcessOptions", "WebViewControlProcessOptions.WebViewControlProcessOptions()");
		}
		#endif
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlProcessOptions.WebViewControlProcessOptions()
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlProcessOptions.EnterpriseId.set
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlProcessOptions.EnterpriseId.get
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlProcessOptions.PrivateNetworkClientServerCapability.set
		// Forced skipping of method Windows.Web.UI.Interop.WebViewControlProcessOptions.PrivateNetworkClientServerCapability.get
	}
}
