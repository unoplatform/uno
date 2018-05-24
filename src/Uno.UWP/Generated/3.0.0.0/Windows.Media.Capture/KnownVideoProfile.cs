#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Capture
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum KnownVideoProfile 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideoRecording,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HighQualityPhoto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		BalancedVideoAndPhoto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideoConferencing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PhotoSequence,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HighFrameRate,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VariablePhotoSequence,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HdrWithWcgVideo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HdrWithWcgPhoto,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VideoHdr8,
		#endif
	}
	#endif
}
