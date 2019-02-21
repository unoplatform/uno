#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SearchBoxQuerySubmittedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.System.VirtualKeyModifiers KeyModifiers
		{
			get
			{
				throw new global::System.NotImplementedException("The member VirtualKeyModifiers SearchBoxQuerySubmittedEventArgs.KeyModifiers is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Language
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SearchBoxQuerySubmittedEventArgs.Language is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Search.SearchQueryLinguisticDetails LinguisticDetails
		{
			get
			{
				throw new global::System.NotImplementedException("The member SearchQueryLinguisticDetails SearchBoxQuerySubmittedEventArgs.LinguisticDetails is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string QueryText
		{
			get
			{
				throw new global::System.NotImplementedException("The member string SearchBoxQuerySubmittedEventArgs.QueryText is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxQuerySubmittedEventArgs.QueryText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxQuerySubmittedEventArgs.Language.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxQuerySubmittedEventArgs.LinguisticDetails.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SearchBoxQuerySubmittedEventArgs.KeyModifiers.get
	}
}
