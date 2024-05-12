#nullable enable

using System;
using Microsoft.UI.Composition;
using Uno.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter : IBorderInfoProvider
{
	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing;

	Brush IBorderInfoProvider.BorderBrush => BorderBrush;

	Thickness IBorderInfoProvider.BorderThickness => BorderThickness;

	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadius;

#if __SKIA__
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");

	SerialDisposable IBorderInfoProvider.BorderBrushSubscriptionDisposable { get; set; } = new();
	SerialDisposable IBorderInfoProvider.BackgroundBrushSubscriptionDisposable { get; set; } = new();
#endif
}
