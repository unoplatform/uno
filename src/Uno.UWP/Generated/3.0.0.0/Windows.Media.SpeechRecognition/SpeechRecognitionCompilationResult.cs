#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpeechRecognitionCompilationResult 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.SpeechRecognition.SpeechRecognitionResultStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpeechRecognitionResultStatus SpeechRecognitionCompilationResult.Status is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognitionCompilationResult.Status.get
	}
}
