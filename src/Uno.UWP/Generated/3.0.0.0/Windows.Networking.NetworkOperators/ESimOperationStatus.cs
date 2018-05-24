#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ESimOperationStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAuthorized,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PolicyViolation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InsufficientSpaceOnCard,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServerNotReachable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TimeoutWaitingForUserConsent,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		IncorrectConfirmationCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConfirmationCodeMaxRetriesExceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CardRemoved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CardBusy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
	}
	#endif
}
