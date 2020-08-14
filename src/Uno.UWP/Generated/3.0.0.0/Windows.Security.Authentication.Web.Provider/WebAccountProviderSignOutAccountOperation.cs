#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountProviderSignOutAccountOperation : global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation,global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderBaseReportOperation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.WebAccountProviderOperationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountProviderOperationKind WebAccountProviderSignOutAccountOperation.Kind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri ApplicationCallbackUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri WebAccountProviderSignOutAccountOperation.ApplicationCallbackUri is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ClientId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string WebAccountProviderSignOutAccountOperation.ClientId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.WebAccount WebAccount
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccount WebAccountProviderSignOutAccountOperation.WebAccount is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderSignOutAccountOperation.WebAccount.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderSignOutAccountOperation.ApplicationCallbackUri.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderSignOutAccountOperation.ClientId.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderSignOutAccountOperation.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportCompleted()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderSignOutAccountOperation", "void WebAccountProviderSignOutAccountOperation.ReportCompleted()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportError( global::Windows.Security.Authentication.Web.Core.WebProviderError value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderSignOutAccountOperation", "void WebAccountProviderSignOutAccountOperation.ReportError(WebProviderError value)");
		}
		#endif
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderBaseReportOperation
	}
}
