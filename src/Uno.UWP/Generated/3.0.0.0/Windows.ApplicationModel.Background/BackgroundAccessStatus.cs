#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Background
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum BackgroundAccessStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unspecified,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllowedWithAlwaysOnRealTimeConnectivity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllowedMayUseActiveRealTimeConnectivity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Denied,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlwaysAllowed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllowedSubjectToSystemPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeniedBySystemPolicy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeniedByUser,
		#endif
	}
	#endif
}
