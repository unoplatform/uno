namespace Windows.Media.SpeechRecognition
{
	public enum SpeechRecognitionResultStatus 
	{
		Success,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		TopicLanguageNotSupported,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		GrammarLanguageMismatch,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		GrammarCompilationFailure,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		AudioQualityFailure,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		UserCanceled,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		Unknown,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		TimeoutExceeded,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		PauseLimitExceeded,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		NetworkFailure,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		MicrophoneUnavailable
	}
}
