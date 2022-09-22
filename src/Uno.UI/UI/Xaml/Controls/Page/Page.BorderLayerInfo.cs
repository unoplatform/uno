#nullable enable

using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

public partial class Page : IBorderInfoProvider
{
	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => InternalBackgroundSizing;

	Brush IBorderInfoProvider.BorderBrush => BorderBrush;

	Thickness IBorderInfoProvider.BorderThickness => BorderThickness;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadius;

	object? IBorderInfoProvider.BackgroundImage => null;
}
