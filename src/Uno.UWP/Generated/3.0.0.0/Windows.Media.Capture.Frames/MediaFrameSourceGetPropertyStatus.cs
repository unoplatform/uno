#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture.Frames
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaFrameSourceGetPropertyStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnknownFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DeviceNotAvailable,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MaxPropertyValueSizeTooSmall,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MaxPropertyValueSizeRequired,
		#endif
	}
	#endif
}
