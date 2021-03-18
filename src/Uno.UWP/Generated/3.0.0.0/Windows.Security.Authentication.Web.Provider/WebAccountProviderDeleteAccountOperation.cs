#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountProviderDeleteAccountOperation : global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation,global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderBaseReportOperation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.WebAccount WebAccount
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccount WebAccountProviderDeleteAccountOperation.WebAccount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.WebAccountProviderOperationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountProviderOperationKind WebAccountProviderDeleteAccountOperation.Kind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderDeleteAccountOperation.WebAccount.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderDeleteAccountOperation.Kind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportCompleted()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderDeleteAccountOperation", "void WebAccountProviderDeleteAccountOperation.ReportCompleted()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportError( global::Windows.Security.Authentication.Web.Core.WebProviderError value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderDeleteAccountOperation", "void WebAccountProviderDeleteAccountOperation.ReportError(WebProviderError value)");
		}
		#endif
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderBaseReportOperation
	}
}
