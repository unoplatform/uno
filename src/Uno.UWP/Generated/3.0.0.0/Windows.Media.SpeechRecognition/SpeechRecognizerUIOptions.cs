#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpeechRecognizerUIOptions 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool ShowConfirmation
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpeechRecognizerUIOptions.ShowConfirmation is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions", "bool SpeechRecognizerUIOptions.ShowConfirmation");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsReadBackEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SpeechRecognizerUIOptions.IsReadBackEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions", "bool SpeechRecognizerUIOptions.IsReadBackEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string ExampleText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpeechRecognizerUIOptions.ExampleText is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions", "string SpeechRecognizerUIOptions.ExampleText");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  string AudiblePrompt
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SpeechRecognizerUIOptions.AudiblePrompt is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions", "string SpeechRecognizerUIOptions.AudiblePrompt");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.ExampleText.get
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.ExampleText.set
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.AudiblePrompt.get
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.AudiblePrompt.set
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.IsReadBackEnabled.get
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.IsReadBackEnabled.set
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.ShowConfirmation.get
		// Forced skipping of method Windows.Media.SpeechRecognition.SpeechRecognizerUIOptions.ShowConfirmation.set
	}
}
