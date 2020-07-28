#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SelectableWordsSegmenter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ResolvedLanguage
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SelectableWordsSegmenter.ResolvedLanguage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SelectableWordsSegmenter( string language) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Text.SelectableWordsSegmenter", "SelectableWordsSegmenter.SelectableWordsSegmenter(string language)");
		}
		#endif
		// Forced skipping of method Windows.Data.Text.SelectableWordsSegmenter.SelectableWordsSegmenter(string)
		// Forced skipping of method Windows.Data.Text.SelectableWordsSegmenter.ResolvedLanguage.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Data.Text.SelectableWordSegment GetTokenAt( string text,  uint startIndex)
		{
			throw new global::System.NotImplementedException("The member SelectableWordSegment SelectableWordsSegmenter.GetTokenAt(string text, uint startIndex) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Data.Text.SelectableWordSegment> GetTokens( string text)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<SelectableWordSegment> SelectableWordsSegmenter.GetTokens(string text) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Tokenize( string text,  uint startIndex,  global::Windows.Data.Text.SelectableWordSegmentsTokenizingHandler handler)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Text.SelectableWordsSegmenter", "void SelectableWordsSegmenter.Tokenize(string text, uint startIndex, SelectableWordSegmentsTokenizingHandler handler)");
		}
		#endif
	}
}
