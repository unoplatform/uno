#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IWebAccountProviderTokenObjects2 : global::Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenObjects
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.System.User User
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Security.Authentication.Web.Provider.IWebAccountProviderTokenObjects2.User.get
	}
}
