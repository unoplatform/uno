#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MapRouteFinderStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidCredentials,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoRouteFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoRouteFoundWithGivenOptions,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StartPointNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EndPointNotFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoPedestrianRouteFound,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotSupported,
		#endif
	}
	#endif
}
