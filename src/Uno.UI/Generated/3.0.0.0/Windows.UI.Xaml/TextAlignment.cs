#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml
{
	#if false || false || false || false || false || false || false
	public   enum TextAlignment 
	{
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Center
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Left
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		Start = 1,
		#endif
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Right
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		End = 2,
		#endif
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.Justify
		// Skipping already declared field Windows.UI.Xaml.TextAlignment.DetectFromContent
	}
	#endif
}
