#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.SpeechRecognition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class VoiceCommandSet 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VoiceCommandSet.Language is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20VoiceCommandSet.Language");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Name
		{
			get
			{
				throw new global::System.NotImplementedException("The member string VoiceCommandSet.Name is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20VoiceCommandSet.Name");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.SpeechRecognition.VoiceCommandSet.Language.get
		// Forced skipping of method Windows.Media.SpeechRecognition.VoiceCommandSet.Name.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SetPhraseListAsync( string phraseListName,  global::System.Collections.Generic.IEnumerable<string> phraseList)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction VoiceCommandSet.SetPhraseListAsync(string phraseListName, IEnumerable<string> phraseList) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20VoiceCommandSet.SetPhraseListAsync%28string%20phraseListName%2C%20IEnumerable%3Cstring%3E%20phraseList%29");
		}
		#endif
	}
}
