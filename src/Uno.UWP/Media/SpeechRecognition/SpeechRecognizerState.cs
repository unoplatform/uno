namespace Windows.Media.SpeechRecognition
{
	public enum SpeechRecognizerState 
	{
		Idle,

		Capturing,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		Processing,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		SoundStarted,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		SoundEnded,

		SpeechDetected,

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__
		[global::Uno.NotImplemented]
		#endif
		Paused
	}
}
