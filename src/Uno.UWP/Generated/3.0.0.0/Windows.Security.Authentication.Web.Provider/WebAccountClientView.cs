#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountClientView 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string AccountPairwiseId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebAccountClientView.AccountPairwiseId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri ApplicationCallbackUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebAccountClientView.ApplicationCallbackUri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.WebAccountClientViewType Type
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountClientViewType WebAccountClientView.Type is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebAccountClientView( global::Windows.Security.Authentication.Web.Provider.WebAccountClientViewType viewType,  global::System.Uri applicationCallbackUri) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountClientView", "WebAccountClientView.WebAccountClientView(WebAccountClientViewType viewType, Uri applicationCallbackUri)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountClientView.WebAccountClientView(Windows.Security.Authentication.Web.Provider.WebAccountClientViewType, System.Uri)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public WebAccountClientView( global::Windows.Security.Authentication.Web.Provider.WebAccountClientViewType viewType,  global::System.Uri applicationCallbackUri,  string accountPairwiseId) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountClientView", "WebAccountClientView.WebAccountClientView(WebAccountClientViewType viewType, Uri applicationCallbackUri, string accountPairwiseId)");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountClientView.WebAccountClientView(Windows.Security.Authentication.Web.Provider.WebAccountClientViewType, System.Uri, string)
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountClientView.ApplicationCallbackUri.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountClientView.Type.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountClientView.AccountPairwiseId.get
	}
}
