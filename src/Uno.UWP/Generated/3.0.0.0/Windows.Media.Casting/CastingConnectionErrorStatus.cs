#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Casting
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CastingConnectionErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Succeeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceDidNotRespond,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceLocked,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ProtectedPlaybackFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidCastingSource,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
	}
	#endif
}
