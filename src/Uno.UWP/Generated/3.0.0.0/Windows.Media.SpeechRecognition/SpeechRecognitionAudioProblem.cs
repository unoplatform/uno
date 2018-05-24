#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpeechRecognitionAudioProblem 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		None,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooNoisy,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NoSignal,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooLoud,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooQuiet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooFast,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TooSlow,
		#endif
	}
	#endif
}
