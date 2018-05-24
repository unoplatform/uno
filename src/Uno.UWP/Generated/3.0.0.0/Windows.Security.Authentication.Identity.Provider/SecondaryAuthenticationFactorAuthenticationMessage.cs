#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SecondaryAuthenticationFactorAuthenticationMessage 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Invalid,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SwipeUpWelcome,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TapWelcome,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceNeedsAttention,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LookingForDevice,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LookingForDevicePluggedin,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BluetoothIsDisabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NfcIsDisabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WiFiIsDisabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ExtraTapIsRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DisabledByPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TapOnDeviceRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HoldFinger,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ScanFinger,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnauthorizedUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReregisterRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TryAgain,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SayPassphrase,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReadyToSignIn,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UseAnotherSignInOption,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TimeLimitExceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CanceledByUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CenterHand,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MoveHandCloser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MoveHandFarther,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PlaceHandAbove,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RecognitionFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceUnavailable,
		#endif
	}
	#endif
}
