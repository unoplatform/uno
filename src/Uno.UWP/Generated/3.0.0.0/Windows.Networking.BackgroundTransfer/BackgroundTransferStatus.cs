#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BackgroundTransferStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Idle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Running,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedByApplication,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedCostedNetwork,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedNoNetwork,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Completed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Canceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Error,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedRecoverableWebErrorStatus,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedSystemPolicy,
		#endif
	}
	#endif
}
