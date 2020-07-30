#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SearchBoxSuggestionsRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SearchBoxSuggestionsRequestedEventArgs.Language is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Search.SearchQueryLinguisticDetails LinguisticDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member SearchQueryLinguisticDetails SearchBoxSuggestionsRequestedEventArgs.LinguisticDetails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string QueryText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SearchBoxSuggestionsRequestedEventArgs.QueryText is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Search.SearchSuggestionsRequest Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member SearchSuggestionsRequest SearchBoxSuggestionsRequestedEventArgs.Request is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxSuggestionsRequestedEventArgs.QueryText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxSuggestionsRequestedEventArgs.Language.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxSuggestionsRequestedEventArgs.LinguisticDetails.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxSuggestionsRequestedEventArgs.Request.get
	}
}
