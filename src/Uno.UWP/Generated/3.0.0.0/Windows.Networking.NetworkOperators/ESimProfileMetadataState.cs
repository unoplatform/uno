#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.NetworkOperators
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum ESimProfileMetadataState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WaitingForInstall,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Downloading,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Installing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Expired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RejectingDownload,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoLongerAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeniedByPolicy,
		#endif
	}
	#endif
}
