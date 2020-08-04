#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if false || false || false || false || false || false || false
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public   enum AppViewBackButtonVisibility 
	{
		// Skipping already declared field Windows.UI.Core.AppViewBackButtonVisibility.Visible
		// Skipping already declared field Windows.UI.Core.AppViewBackButtonVisibility.Collapsed
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		Disabled,
		#endif
	}
	#endif
}
