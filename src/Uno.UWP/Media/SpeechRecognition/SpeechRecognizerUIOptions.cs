namespace Windows.Media.SpeechRecognition
{
	public partial class SpeechRecognizerUIOptions
	{
		public bool ShowConfirmation { get; set; }

#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__
		[global::Uno.NotImplemented]
#endif
		public bool IsReadBackEnabled { get; set; }

		public string? ExampleText { get; set; }

		public string? AudiblePrompt { get; set; }
	}
}
