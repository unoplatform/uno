#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebProviderTokenResponse 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Core.WebTokenResponse ClientResponse
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebTokenResponse WebProviderTokenResponse.ClientResponse is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebProviderTokenResponse( global::Windows.Security.Authentication.Web.Core.WebTokenResponse webTokenResponse) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebProviderTokenResponse", "WebProviderTokenResponse.WebProviderTokenResponse(WebTokenResponse webTokenResponse)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenResponse.WebProviderTokenResponse(Windows.Security.Authentication.Web.Core.WebTokenResponse)
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebProviderTokenResponse.ClientResponse.get
	}
}
