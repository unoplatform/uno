#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpeechRecognizerStateChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.Media.SpeechRecognition.SpeechRecognizerState State
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpeechRecognizerState SpeechRecognizerStateChangedEventArgs.State is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerStateChangedEventArgs.State.get
	}
}
