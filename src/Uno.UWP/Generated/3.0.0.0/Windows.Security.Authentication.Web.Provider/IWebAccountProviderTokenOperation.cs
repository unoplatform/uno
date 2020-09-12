#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWebAccountProviderTokenOperation : global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.DateTimeOffset CacheExpirationTime
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest ProviderRequest
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IList<global::Windows.Security.Authentication.Web.Provider.WebProviderTokenResponse> ProviderResponses
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenOperation.ProviderRequest.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenOperation.ProviderResponses.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenOperation.CacheExpirationTime.set
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenOperation.CacheExpirationTime.get
	}
}
