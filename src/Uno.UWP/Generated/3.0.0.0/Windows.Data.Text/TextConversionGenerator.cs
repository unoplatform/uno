#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TextConversionGenerator 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool LanguageAvailableButNotInstalled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool TextConversionGenerator.LanguageAvailableButNotInstalled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ResolvedLanguage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TextConversionGenerator.ResolvedLanguage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public TextConversionGenerator( string languageTag) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Text.TextConversionGenerator", "TextConversionGenerator.TextConversionGenerator(string languageTag)");
		}
		#endif
		// Forced skipping of method Windows.Data.Text.TextConversionGenerator.TextConversionGenerator(string)
		// Forced skipping of method Windows.Data.Text.TextConversionGenerator.ResolvedLanguage.get
		// Forced skipping of method Windows.Data.Text.TextConversionGenerator.LanguageAvailableButNotInstalled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<string>> GetCandidatesAsync( string input)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<string>> TextConversionGenerator.GetCandidatesAsync(string input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<string>> GetCandidatesAsync( string input,  uint maxCandidates)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<string>> TextConversionGenerator.GetCandidatesAsync(string input, uint maxCandidates) is not implemented in Uno.");
		}
		#endif
	}
}
