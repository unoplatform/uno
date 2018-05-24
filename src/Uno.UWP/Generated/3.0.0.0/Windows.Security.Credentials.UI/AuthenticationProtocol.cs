#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Credentials.UI
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AuthenticationProtocol 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Basic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Digest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Ntlm,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Kerberos,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Negotiate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CredSsp,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Custom,
		#endif
	}
	#endif
}
