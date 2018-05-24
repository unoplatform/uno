#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpeechRecognitionScenario 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		WebSearch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Dictation,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FormFilling,
		#endif
	}
	#endif
}
