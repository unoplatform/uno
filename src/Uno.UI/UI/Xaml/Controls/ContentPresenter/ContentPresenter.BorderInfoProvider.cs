using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class ContentPresenter : IBorderInfoProvider
{
	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing;

	Brush IBorderInfoProvider.BorderBrush => BorderBrush;

	Thickness IBorderInfoProvider.BorderThickness => BorderThickness;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadius;

#if __ANDROID__
	bool IBorderInfoProvider.ShouldUpdateMeasures { get; set; }
#endif
}
