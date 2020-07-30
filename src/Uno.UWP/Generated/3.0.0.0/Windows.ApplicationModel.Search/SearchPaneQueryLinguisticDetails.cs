#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Search
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SearchPaneQueryLinguisticDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> QueryTextAlternatives
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> SearchPaneQueryLinguisticDetails.QueryTextAlternatives is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint QueryTextCompositionLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SearchPaneQueryLinguisticDetails.QueryTextCompositionLength is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint QueryTextCompositionStart
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SearchPaneQueryLinguisticDetails.QueryTextCompositionStart is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Search.SearchPaneQueryLinguisticDetails.QueryTextAlternatives.get
		// Forced skipping of method Windows.ApplicationModel.Search.SearchPaneQueryLinguisticDetails.QueryTextCompositionStart.get
		// Forced skipping of method Windows.ApplicationModel.Search.SearchPaneQueryLinguisticDetails.QueryTextCompositionLength.get
	}
}
