#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false || false || false || false
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public   enum TextAlignment 
	{
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Center
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Left
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		Start,
		#endif
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Right
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		End,
		#endif
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Justify
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.DetectFromContent
	}
	#endif
}
