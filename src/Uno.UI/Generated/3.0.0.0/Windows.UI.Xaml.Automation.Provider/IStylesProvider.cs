#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IStylesProvider 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ExtendedProperties
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Color FillColor
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.UI.Color FillPatternColor
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string FillPatternStyle
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string Shape
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int StyleId
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string StyleName
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IStylesProvider.ExtendedProperties.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IStylesProvider.FillColor.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IStylesProvider.FillPatternColor.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IStylesProvider.FillPatternStyle.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IStylesProvider.Shape.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IStylesProvider.StyleId.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.IStylesProvider.StyleName.get
	}
}
