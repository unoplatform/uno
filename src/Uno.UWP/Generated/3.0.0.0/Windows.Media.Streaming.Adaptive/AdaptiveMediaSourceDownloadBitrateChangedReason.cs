#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Streaming.Adaptive
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum AdaptiveMediaSourceDownloadBitrateChangedReason 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SufficientInboundBitsPerSecond,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InsufficientInboundBitsPerSecond,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		LowBufferLevel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PositionChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TrackSelectionChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DesiredBitratesChanged,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ErrorInPreviousBitrate,
		#endif
	}
	#endif
}
