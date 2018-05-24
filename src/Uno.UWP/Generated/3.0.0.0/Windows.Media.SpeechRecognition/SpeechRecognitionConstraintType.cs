#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpeechRecognitionConstraintType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Topic,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		List,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Grammar,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		VoiceCommandDefinition,
		#endif
	}
	#endif
}
