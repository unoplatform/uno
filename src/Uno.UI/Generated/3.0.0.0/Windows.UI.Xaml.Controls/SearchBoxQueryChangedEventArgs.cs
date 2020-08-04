#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SearchBoxQueryChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SearchBoxQueryChangedEventArgs.Language is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Search.SearchQueryLinguisticDetails LinguisticDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member SearchQueryLinguisticDetails SearchBoxQueryChangedEventArgs.LinguisticDetails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string QueryText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SearchBoxQueryChangedEventArgs.QueryText is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxQueryChangedEventArgs.QueryText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxQueryChangedEventArgs.Language.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxQueryChangedEventArgs.LinguisticDetails.get
	}
}
