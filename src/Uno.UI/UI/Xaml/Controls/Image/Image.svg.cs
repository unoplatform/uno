#nullable enable

namespace Windows.UI.Xaml.Controls;

partial class Image
{
#if __ANDROID__ || __IOS__ || __MACOS__ || __SKIA__
	private UIElement? _svgCanvas;
#endif
}
