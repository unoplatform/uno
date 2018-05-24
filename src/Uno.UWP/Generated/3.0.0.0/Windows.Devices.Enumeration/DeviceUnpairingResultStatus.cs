#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Enumeration
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum DeviceUnpairingResultStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unpaired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlreadyUnpaired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OperationAlreadyInProgress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AccessDenied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Failed,
		#endif
	}
	#endif
}
