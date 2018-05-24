#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpeechRecognizerTimeouts 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan InitialSilenceTimeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SpeechRecognizerTimeouts.InitialSilenceTimeout is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts", "TimeSpan SpeechRecognizerTimeouts.InitialSilenceTimeout");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan EndSilenceTimeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SpeechRecognizerTimeouts.EndSilenceTimeout is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts", "TimeSpan SpeechRecognizerTimeouts.EndSilenceTimeout");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan BabbleTimeout
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SpeechRecognizerTimeouts.BabbleTimeout is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts", "TimeSpan SpeechRecognizerTimeouts.BabbleTimeout");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts.InitialSilenceTimeout.get
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts.InitialSilenceTimeout.set
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts.EndSilenceTimeout.get
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts.EndSilenceTimeout.set
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts.BabbleTimeout.get
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerTimeouts.BabbleTimeout.set
	}
}
