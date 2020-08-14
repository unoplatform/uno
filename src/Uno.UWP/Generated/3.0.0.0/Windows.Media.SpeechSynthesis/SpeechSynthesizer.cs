#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechSynthesis
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SpeechSynthesizer : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.SpeechSynthesis.VoiceInformation Voice
		{
			get
			{
				throw new global::System.NotImplementedException("The member VoiceInformation SpeechSynthesizer.Voice is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechSynthesis.SpeechSynthesizer", "VoiceInformation SpeechSynthesizer.Voice");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.SpeechSynthesis.SpeechSynthesizerOptions Options
		{
			get
			{
				throw new global::System.NotImplementedException("The member SpeechSynthesizerOptions SpeechSynthesizer.Options is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.SpeechSynthesis.VoiceInformation> AllVoices
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<VoiceInformation> SpeechSynthesizer.AllVoices is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Media.SpeechSynthesis.VoiceInformation DefaultVoice
		{
			get
			{
				throw new global::System.NotImplementedException("The member VoiceInformation SpeechSynthesizer.DefaultVoice is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SpeechSynthesizer() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechSynthesis.SpeechSynthesizer", "SpeechSynthesizer.SpeechSynthesizer()");
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechSynthesis.SpeechSynthesizer.SpeechSynthesizer()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.SpeechSynthesis.SpeechSynthesisStream> SynthesizeTextToStreamAsync( string text)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpeechSynthesisStream> SpeechSynthesizer.SynthesizeTextToStreamAsync(string text) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.Media.SpeechSynthesis.SpeechSynthesisStream> SynthesizeSsmlToStreamAsync( string Ssml)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<SpeechSynthesisStream> SpeechSynthesizer.SynthesizeSsmlToStreamAsync(string Ssml) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechSynthesis.SpeechSynthesizer.Voice.set
		// Forced skipping of method Windows.Media.SpeechSynthesis.SpeechSynthesizer.Voice.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.SpeechSynthesis.SpeechSynthesizer", "void SpeechSynthesizer.Dispose()");
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechSynthesis.SpeechSynthesizer.Options.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<bool> TrySetDefaultVoiceAsync( global::Windows.Media.SpeechSynthesis.VoiceInformation voice)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> SpeechSynthesizer.TrySetDefaultVoiceAsync(VoiceInformation voice) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices.get
		// Forced skipping of method Windows.Media.SpeechSynthesis.SpeechSynthesizer.DefaultVoice.get
		// Processing: System.IDisposable
	}
}
