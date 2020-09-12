#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISpeechRecognitionConstraint 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsEnabled
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.SpeechRecognition.SpeechRecognitionConstraintProbability Probability
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Tag
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Media.SpeechRecognition.SpeechRecognitionConstraintType Type
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.ISpeechRecognitionConstraint.IsEnabled.get
		// Forced skipping of method Windows.Media.SpeechRecognition.ISpeechRecognitionConstraint.IsEnabled.set
		// Forced skipping of method Windows.Media.SpeechRecognition.ISpeechRecognitionConstraint.Tag.get
		// Forced skipping of method Windows.Media.SpeechRecognition.ISpeechRecognitionConstraint.Tag.set
		// Forced skipping of method Windows.Media.SpeechRecognition.ISpeechRecognitionConstraint.Type.get
		// Forced skipping of method Windows.Media.SpeechRecognition.ISpeechRecognitionConstraint.Probability.get
		// Forced skipping of method Windows.Media.SpeechRecognition.ISpeechRecognitionConstraint.Probability.set
	}
}
