using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

internal record struct BorderLayerState(
	Brush Background,
	BackgroundSizing BackgroundSizing,
	Brush BorderBrush,
	Thickness BorderThickness,
	CornerRadius CornerRadius)
{
}
