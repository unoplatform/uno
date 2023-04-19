#nullable enable

using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

internal record struct BorderLayerState(
	Rect Area,
	Brush? Background,
	BackgroundSizing BackgroundSizing,
	Brush? BorderBrush,
	Thickness BorderThickness,
	CornerRadius CornerRadius)
{
	internal BorderLayerState(Rect area, IBorderInfoProvider borderInfoProvider) : this(
		Area,
		borderInfoProvider.Background,
		borderInfoProvider.BackgroundSizing,
		borderInfoProvider.BorderBrush,
		borderInfoProvider.BorderThickness,
		borderInfoProvider.CornerRadius)
	{
	}
}
