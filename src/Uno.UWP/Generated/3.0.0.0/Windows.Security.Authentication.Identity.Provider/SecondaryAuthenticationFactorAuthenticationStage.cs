#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SecondaryAuthenticationFactorAuthenticationStage 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotStarted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WaitingForUserConfirmation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CollectingCredential,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SuspendingAuthentication,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CredentialCollected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CredentialAuthenticated,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StoppingAuthentication,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReadyForLock,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CheckingDevicePresence,
		#endif
	}
	#endif
}
