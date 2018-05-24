#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpeechRecognizerState 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Idle,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Capturing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Processing,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SoundStarted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SoundEnded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SpeechDetected,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Paused,
		#endif
	}
	#endif
}
