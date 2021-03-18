#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpeechContinuousRecognitionResultGeneratedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.SpeechRecognition.SpeechRecognitionResult Result
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpeechRecognitionResult SpeechContinuousRecognitionResultGeneratedEventArgs.Result is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechContinuousRecognitionResultGeneratedEventArgs.Result.get
	}
}
