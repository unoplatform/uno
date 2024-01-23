#nullable enable

using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Represents a state of border layer.
/// </summary>
/// <param name="ElementSize">Element size.</param>
/// <param name="Background">Background brush.</param>
/// <param name="BackgroundSizing">Background sizing.</param>
/// <param name="BorderBrush">Border brush.</param>
/// <param name="BorderThickness">Border thickness.</param>
/// <param name="CornerRadius">Corner radius.</param>
internal record struct BorderLayerState(
	Size ElementSize,
	Brush? Background,
	BackgroundSizing BackgroundSizing,
	Brush? BorderBrush,
	Thickness BorderThickness,
	CornerRadius CornerRadius)
{
	internal BorderLayerState(Size elementSize, IBorderInfoProvider borderInfoProvider) : this(
		elementSize,
		borderInfoProvider.Background,
		borderInfoProvider.BackgroundSizing,
		borderInfoProvider.BorderBrush,
		borderInfoProvider.BorderThickness,
		borderInfoProvider.CornerRadius)
	{
	}
}
