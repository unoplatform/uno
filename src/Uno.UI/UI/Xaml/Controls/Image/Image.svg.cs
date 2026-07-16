#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class Image
{
#if __SKIA__
	private UIElement? _svgCanvas;
#endif
}
