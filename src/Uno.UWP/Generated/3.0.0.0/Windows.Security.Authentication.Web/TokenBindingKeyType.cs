#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Web
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum TokenBindingKeyType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Rsa2048,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EcdsaP256,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AnyExisting,
		#endif
	}
	#endif
}
