#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Store.Preview.InstallControl
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AppInstallState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Pending,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Starting,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AcquiringLicense,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Downloading,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		RestoringData,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Installing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Completed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Canceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paused,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Error,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedLowBattery,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedWiFiRecommended,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PausedWiFiRequired,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ReadyToDownload,
		#endif
	}
	#endif
}
