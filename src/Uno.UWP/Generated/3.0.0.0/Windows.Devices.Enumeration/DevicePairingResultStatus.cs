#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DevicePairingResultStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotReadyToPair,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotPaired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlreadyPaired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionRejected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooManyConnections,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HardwareFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AuthenticationTimeout,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AuthenticationNotAllowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AuthenticationFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoSupportedProfiles,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtectionLevelCouldNotBeMet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccessDenied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidCeremonyData,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PairingCanceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationAlreadyInProgress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RequiredHandlerNotRegistered,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RejectedByHandler,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RemoteDeviceHasAssociation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failed,
		#endif
	}
	#endif
}
