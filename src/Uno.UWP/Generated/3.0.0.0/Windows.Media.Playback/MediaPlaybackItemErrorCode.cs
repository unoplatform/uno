#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Playback
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaPlaybackItemErrorCode 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aborted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DecodeError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SourceNotSupportedError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EncryptionError,
		#endif
	}
	#endif
}
