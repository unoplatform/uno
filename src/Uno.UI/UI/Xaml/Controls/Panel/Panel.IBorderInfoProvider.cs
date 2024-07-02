#nullable enable

using System;
using Windows.UI.Composition;
using Uno.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Disposables;

namespace Windows.UI.Xaml.Controls;

partial class Panel : IBorderInfoProvider
{
	Brush? IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => InternalBackgroundSizing;

	Brush? IBorderInfoProvider.BorderBrush => BorderBrushInternal;

	Thickness IBorderInfoProvider.BorderThickness => BorderThicknessInternal;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadiusInternal;

#if UNO_HAS_BORDER_VISUAL
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");

	SerialDisposable IBorderInfoProvider.BorderBrushSubscriptionDisposable { get; set; } = new();
	SerialDisposable IBorderInfoProvider.BackgroundBrushSubscriptionDisposable { get; set; } = new();
#endif
}
