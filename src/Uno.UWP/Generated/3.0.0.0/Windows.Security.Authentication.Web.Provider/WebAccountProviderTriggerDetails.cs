#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class WebAccountProviderTriggerDetails : global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenObjects,global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenObjects2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderOperation Operation
		{
			get
			{
				throw new global::System.NotImplementedException("The member IWebAccountProviderOperation WebAccountProviderTriggerDetails.Operation is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.User User
		{
			get
			{
				throw new global::System.NotImplementedException("The member User WebAccountProviderTriggerDetails.User is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderTriggerDetails.Operation.get
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.WebAccountProviderTriggerDetails.User.get
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenObjects
		// Processing: Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenObjects2
	}
}
