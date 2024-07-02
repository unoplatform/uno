#nullable enable

using System;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

public partial class Page : IBorderInfoProvider
{
	Brush? IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => InternalBackgroundSizing;

	Brush? IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => Thickness.Empty;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadius.None;

#if UNO_HAS_BORDER_VISUAL
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");

	SerialDisposable IBorderInfoProvider.BorderBrushSubscriptionDisposable { get; set; } = new();
	SerialDisposable IBorderInfoProvider.BackgroundBrushSubscriptionDisposable { get; set; } = new();
#endif
}
