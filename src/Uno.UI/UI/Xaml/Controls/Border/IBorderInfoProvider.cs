#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Xaml.Controls;

internal partial interface IBorderInfoProvider
{
	Brush? Background { get; }

	BackgroundSizing BackgroundSizing { get; }

	Brush? BorderBrush { get; }

	Thickness BorderThickness { get; }

	CornerRadius CornerRadius { get; }

	object? BackgroundImage { get; }
}
