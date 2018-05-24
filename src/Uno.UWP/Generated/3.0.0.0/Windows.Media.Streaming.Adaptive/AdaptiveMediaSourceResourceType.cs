#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Streaming.Adaptive
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AdaptiveMediaSourceResourceType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Manifest,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InitializationSegment,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MediaSegment,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Key,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InitializationVector,
		#endif
	}
	#endif
}
