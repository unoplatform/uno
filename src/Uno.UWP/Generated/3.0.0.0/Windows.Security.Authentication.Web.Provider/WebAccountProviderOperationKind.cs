#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum WebAccountProviderOperationKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequestToken,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GetTokenSilently,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddAccount,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ManageAccount,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeleteAccount,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RetrieveCookies,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SignOutAccount,
		#endif
	}
	#endif
}
