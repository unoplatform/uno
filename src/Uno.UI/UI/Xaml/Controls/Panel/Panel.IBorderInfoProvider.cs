#nullable enable

using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class Panel : IBorderInfoProvider
{
	Brush? IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => InternalBackgroundSizing;

	Brush? IBorderInfoProvider.BorderBrush => BorderBrushInternal;

	Thickness IBorderInfoProvider.BorderThickness => BorderThicknessInternal;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadiusInternal;

#if __SKIA__
	SerialDisposable IBorderInfoProvider.BorderBrushSubscriptionDisposable { get; set; } = new();
	SerialDisposable IBorderInfoProvider.BackgroundBrushSubscriptionDisposable { get; set; } = new();
#endif
}
