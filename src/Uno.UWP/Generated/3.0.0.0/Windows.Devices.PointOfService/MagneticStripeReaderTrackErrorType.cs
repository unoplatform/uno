#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.PointOfService
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MagneticStripeReaderTrackErrorType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		StartSentinelError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EndSentinelError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ParityError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LrcError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
	}
	#endif
}
