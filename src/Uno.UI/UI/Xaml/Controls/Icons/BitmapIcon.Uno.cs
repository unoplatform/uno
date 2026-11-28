#nullable enable

using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class BitmapIcon
{
	// WinUI only reads ShowAsMonochrome when the image is (re)decoded, Uno applies it right away.
	private void OnShowAsMonochromeChanged() => ApplyForegroundColorToImage(Foreground as SolidColorBrush);
}
