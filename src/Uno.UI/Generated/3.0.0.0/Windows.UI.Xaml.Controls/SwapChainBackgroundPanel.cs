#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SwapChainBackgroundPanel : global::Windows.UI.Xaml.Controls.Grid
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SwapChainBackgroundPanel() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SwapChainBackgroundPanel", "SwapChainBackgroundPanel.SwapChainBackgroundPanel()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SwapChainBackgroundPanel.SwapChainBackgroundPanel()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreIndependentInputSource CreateCoreIndependentInputSource( global::Windows.UI.Core.CoreInputDeviceTypes deviceTypes)
		{
			throw new global::System.NotImplementedException("The member CoreIndependentInputSource SwapChainBackgroundPanel.CreateCoreIndependentInputSource(CoreInputDeviceTypes deviceTypes) is not implemented in Uno.");
		}
		#endif
	}
}
