#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IGridItemProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int Column
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int ColumnSpan
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Xaml.Automation.Provider.IRawElementProviderSimple ContainingGrid
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int Row
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int RowSpan
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IGridItemProvider.Column.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IGridItemProvider.ColumnSpan.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IGridItemProvider.ContainingGrid.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IGridItemProvider.Row.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IGridItemProvider.RowSpan.get
	}
}
