#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.OnlineId
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CredentialPromptType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PromptIfNeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RetypeCredentials,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DoNotPrompt,
		#endif
	}
	#endif
}
