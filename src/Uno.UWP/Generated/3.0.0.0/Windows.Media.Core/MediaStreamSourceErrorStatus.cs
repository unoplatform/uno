#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum MediaStreamSourceErrorStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Other,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		OutOfMemory,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FailedToOpenFile,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FailedToConnectToServer,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ConnectionToServerLost,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnspecifiedNetworkError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DecodeError,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UnsupportedMediaFormat,
		#endif
	}
	#endif
}
