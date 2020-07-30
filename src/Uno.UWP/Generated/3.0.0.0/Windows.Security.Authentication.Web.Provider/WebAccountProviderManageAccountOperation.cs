#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountProviderManageAccountOperation : global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Credentials.WebAccount WebAccount
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccount WebAccountProviderManageAccountOperation.WebAccount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.WebAccountProviderOperationKind Kind
		{
			get
			{
				throw new global::System.NotImplementedException("The member WebAccountProviderOperationKind WebAccountProviderManageAccountOperation.Kind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderManageAccountOperation.WebAccount.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void ReportCompleted()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Security.Authentication.Web.Provider.WebAccountProviderManageAccountOperation", "void WebAccountProviderManageAccountOperation.ReportCompleted()");
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderManageAccountOperation.Kind.get
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation
	}
}
