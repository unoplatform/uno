#nullable enable

using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

public partial class Page : IBorderInfoProvider
{
	Brush? IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

	Brush? IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => Thickness.Empty;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadius.None;

#if __ANDROID__
	bool IBorderInfoProvider.ShouldUpdateMeasures { get; set; }
#endif
}
