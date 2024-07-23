#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class Image
{
#if __ANDROID__ || __APPLE_UIKIT__ || __MACOS__ || __SKIA__
	private UIElement? _svgCanvas;
#endif
}
