#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Search
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SearchSuggestionsRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCanceled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool SearchSuggestionsRequest.IsCanceled is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20SearchSuggestionsRequest.IsCanceled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Search.SearchSuggestionCollection SearchSuggestionCollection
		{
			get
			{
				throw new global::System.NotImplementedException("The member SearchSuggestionCollection SearchSuggestionsRequest.SearchSuggestionCollection is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SearchSuggestionCollection%20SearchSuggestionsRequest.SearchSuggestionCollection");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Search.SearchSuggestionsRequest.IsCanceled.get
		// Forced skipping of method Windows.ApplicationModel.Search.SearchSuggestionsRequest.SearchSuggestionCollection.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Search.SearchSuggestionsRequestDeferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member SearchSuggestionsRequestDeferral SearchSuggestionsRequest.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=SearchSuggestionsRequestDeferral%20SearchSuggestionsRequest.GetDeferral%28%29");
		}
		#endif
	}
}
