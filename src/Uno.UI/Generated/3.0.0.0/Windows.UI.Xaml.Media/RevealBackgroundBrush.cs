#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RevealBackgroundBrush : global::Windows.UI.Xaml.Media.RevealBrush
	{
		// Forced skipping of method Windows.UI.Xaml.Media.RevealBackgroundBrush.RevealBackgroundBrush()
	}
}
