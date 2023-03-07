using Windows.UI.Xaml.Media;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

internal record struct BorderLayerState(
	Rect Area,
	Brush Background,
	BackgroundSizing BackgroundSizing,
	Brush BorderBrush,
	Thickness BorderThickness,
	CornerRadius CornerRadius)
{
}
