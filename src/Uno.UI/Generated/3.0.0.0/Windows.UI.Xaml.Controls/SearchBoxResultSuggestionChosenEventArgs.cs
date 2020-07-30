#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SearchBoxResultSuggestionChosenEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.VirtualKeyModifiers KeyModifiers
		{
			get
			{
				throw new global::System.NotImplementedException("The member VirtualKeyModifiers SearchBoxResultSuggestionChosenEventArgs.KeyModifiers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Tag
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SearchBoxResultSuggestionChosenEventArgs.Tag is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SearchBoxResultSuggestionChosenEventArgs() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SearchBoxResultSuggestionChosenEventArgs", "SearchBoxResultSuggestionChosenEventArgs.SearchBoxResultSuggestionChosenEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxResultSuggestionChosenEventArgs.SearchBoxResultSuggestionChosenEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxResultSuggestionChosenEventArgs.Tag.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxResultSuggestionChosenEventArgs.KeyModifiers.get
	}
}
