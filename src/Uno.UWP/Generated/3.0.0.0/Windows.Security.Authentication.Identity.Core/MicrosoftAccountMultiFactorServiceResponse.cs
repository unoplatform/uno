#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Authentication.Identity.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MicrosoftAccountMultiFactorServiceResponse 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Error,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoNetworkConnection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ServiceUnavailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TotpSetupDenied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NgcNotSetup,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionAlreadyDenied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionAlreadyApproved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionExpired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NgcNonceExpired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidSessionId,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidSessionType,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidOperation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidStateTransition,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FlowDisabled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SessionNotApproved,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationCanceledByUser,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NgcDisabledByServer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NgcKeyNotFoundOnServer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UIRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceIdChanged,
		#endif
	}
	#endif
}
