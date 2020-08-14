#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Data.Text
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SemanticTextQuery 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SemanticTextQuery( string aqsFilter) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Text.SemanticTextQuery", "SemanticTextQuery.SemanticTextQuery(string aqsFilter)");
		}
		#endif
		// Forced skipping of method Windows.Data.Text.SemanticTextQuery.SemanticTextQuery(string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SemanticTextQuery( string aqsFilter,  string filterLanguage) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Data.Text.SemanticTextQuery", "SemanticTextQuery.SemanticTextQuery(string aqsFilter, string filterLanguage)");
		}
		#endif
		// Forced skipping of method Windows.Data.Text.SemanticTextQuery.SemanticTextQuery(string, string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Data.Text.TextSegment> Find( string content)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<TextSegment> SemanticTextQuery.Find(string content) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Data.Text.TextSegment> FindInProperty( string propertyContent,  string propertyName)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<TextSegment> SemanticTextQuery.FindInProperty(string propertyContent, string propertyName) is not implemented in Uno.");
		}
		#endif
	}
}
