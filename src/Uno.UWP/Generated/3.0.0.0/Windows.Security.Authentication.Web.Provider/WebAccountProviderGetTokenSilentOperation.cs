#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountProviderGetTokenSilentOperation : global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenOperation,global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation,global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderSilentReportOperation,global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderBaseReportOperation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.WebAccountProviderOperationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountProviderOperationKind WebAccountProviderGetTokenSilentOperation.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset CacheExpirationTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset WebAccountProviderGetTokenSilentOperation.CacheExpirationTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation", "DateTimeOffset WebAccountProviderGetTokenSilentOperation.CacheExpirationTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.WebProviderTokenRequest ProviderRequest
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebProviderTokenRequest WebAccountProviderGetTokenSilentOperation.ProviderRequest is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.Security.Authentication.Web.Provider.WebProviderTokenResponse> ProviderResponses
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<WebProviderTokenResponse> WebAccountProviderGetTokenSilentOperation.ProviderResponses is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation.ProviderRequest.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation.ProviderResponses.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation.CacheExpirationTime.set
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation.CacheExpirationTime.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportUserInteractionRequired()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation", "void WebAccountProviderGetTokenSilentOperation.ReportUserInteractionRequired()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportUserInteractionRequired( global::Windows.Security.Authentication.Web.Core.WebProviderError value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation", "void WebAccountProviderGetTokenSilentOperation.ReportUserInteractionRequired(WebProviderError value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportCompleted()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation", "void WebAccountProviderGetTokenSilentOperation.ReportCompleted()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportError( global::Windows.Security.Authentication.Web.Core.WebProviderError value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderGetTokenSilentOperation", "void WebAccountProviderGetTokenSilentOperation.ReportError(WebProviderError value)");
		}
		#endif
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenOperation
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderSilentReportOperation
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderBaseReportOperation
	}
}
