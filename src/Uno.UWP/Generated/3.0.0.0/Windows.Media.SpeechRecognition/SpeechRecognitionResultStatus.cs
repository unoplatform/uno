#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum SpeechRecognitionResultStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Success,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TopicLanguageNotSupported,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GrammarLanguageMismatch,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		GrammarCompilationFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AudioQualityFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserCanceled,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TimeoutExceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PauseLimitExceeded,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NetworkFailure,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		MicrophoneUnavailable,
		#endif
	}
	#endif
}
