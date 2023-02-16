#nullable enable

using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class Panel : IBorderInfoProvider
{
	Brush? IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => InternalBackgroundSizing;

	Brush? IBorderInfoProvider.BorderBrush => BorderBrushInternal;

	Thickness IBorderInfoProvider.BorderThickness => BorderThicknessInternal;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadiusInternal;

#if __ANDROID__
	Thickness IBorderInfoProvider.Padding => PaddingInternal;

	bool IBorderInfoProvider.ShouldUpdateMeasures => false;
#endif
}
