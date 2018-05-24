#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Services.Maps.OfflineMaps
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum OfflineMapPackageQueryStatus 
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
		NetworkFailure,
		#endif
	}
	#endif
}
