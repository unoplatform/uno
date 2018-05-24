#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Provider
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ITextRangeProvider 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Automation.Provider.ITextRangeProvider Clone();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		bool Compare( global::Windows.UI.Xaml.Automation.Provider.ITextRangeProvider textRangeProvider);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		int CompareEndpoints( global::Windows.UI.Xaml.Automation.Text.TextPatternRangeEndpoint endpoint,  global::Windows.UI.Xaml.Automation.Provider.ITextRangeProvider textRangeProvider,  global::Windows.UI.Xaml.Automation.Text.TextPatternRangeEndpoint targetEndpoint);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void ExpandToEnclosingUnit( global::Windows.UI.Xaml.Automation.Text.TextUnit unit);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Automation.Provider.ITextRangeProvider FindAttribute( int attributeId,  object value,  bool backward);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Automation.Provider.ITextRangeProvider FindText( string text,  bool backward,  bool ignoreCase);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		object GetAttributeValue( int attributeId);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void GetBoundingRectangles(out double[] returnValue);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Automation.Provider.IRawElementProviderSimple GetEnclosingElement();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		string GetText( int maxLength);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		int Move( global::Windows.UI.Xaml.Automation.Text.TextUnit unit,  int count);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		int MoveEndpointByUnit( global::Windows.UI.Xaml.Automation.Text.TextPatternRangeEndpoint endpoint,  global::Windows.UI.Xaml.Automation.Text.TextUnit unit,  int count);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void MoveEndpointByRange( global::Windows.UI.Xaml.Automation.Text.TextPatternRangeEndpoint endpoint,  global::Windows.UI.Xaml.Automation.Provider.ITextRangeProvider textRangeProvider,  global::Windows.UI.Xaml.Automation.Text.TextPatternRangeEndpoint targetEndpoint);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void Select();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void AddToSelection();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void RemoveFromSelection();
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		void ScrollIntoView( bool alignToTop);
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		global::Windows.UI.Xaml.Automation.Provider.IRawElementProviderSimple[] GetChildren();
		#endif
	}
}
