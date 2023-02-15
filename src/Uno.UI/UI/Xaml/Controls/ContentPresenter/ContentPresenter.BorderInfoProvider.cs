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

	object IBorderInfoProvider.BackgroundImage => null;

#if __ANDROID__
	Thickness IBorderInfoProvider.Padding => Padding;

	bool IBorderInfoProvider.ShouldUpdateMeasures => false;
#endif
}
