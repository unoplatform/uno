#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TextReverseConversionGenerator 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool LanguageAvailableButNotInstalled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool TextReverseConversionGenerator.LanguageAvailableButNotInstalled is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ResolvedLanguage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string TextReverseConversionGenerator.ResolvedLanguage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public TextReverseConversionGenerator( string languageTag) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Text.TextReverseConversionGenerator", "TextReverseConversionGenerator.TextReverseConversionGenerator(string languageTag)");
		}
		#endif
		// Forced skipping of method Windows.Data.Text.TextReverseConversionGenerator.TextReverseConversionGenerator(string)
		// Forced skipping of method Windows.Data.Text.TextReverseConversionGenerator.ResolvedLanguage.get
		// Forced skipping of method Windows.Data.Text.TextReverseConversionGenerator.LanguageAvailableButNotInstalled.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> ConvertBackAsync( string input)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> TextReverseConversionGenerator.ConvertBackAsync(string input) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.Data.Text.TextPhoneme>> GetPhonemesAsync( string input)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<TextPhoneme>> TextReverseConversionGenerator.GetPhonemesAsync(string input) is not implemented in Uno.");
		}
		#endif
	}
}
