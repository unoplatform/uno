#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Automation.Provider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ITransformProvider2 : global::Windows.UI.Xaml.Automation.Provider.ITransformProvider
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool CanZoom
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		double MaxZoom
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		double MinZoom
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		double ZoomLevel
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.ITransformProvider2.CanZoom.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.ITransformProvider2.ZoomLevel.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.ITransformProvider2.MaxZoom.get
		// Forced skipping of method Windows.UI.Xaml.Automation.Provider.ITransformProvider2.MinZoom.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void Zoom( double zoom);
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		void ZoomByUnit( global::Windows.UI.Xaml.Automation.ZoomUnit zoomUnit);
		#endif
	}
}
