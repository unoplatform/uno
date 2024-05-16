#if __SKIA__
using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollPresenter : IBorderInfoProvider
{
#if !__SKIA__
	private BorderLayerRenderer _borderRenderer;
#endif

#if !__SKIA__
	partial void InitializePartial()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}
#endif

#if __SKIA__
	private protected override ShapeVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();
#endif

	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

	Brush IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => default;

	CornerRadius IBorderInfoProvider.CornerRadius => default;

#if __SKIA__
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");

	SerialDisposable IBorderInfoProvider.BorderBrushSubscriptionDisposable { get; set; } = new();
	SerialDisposable IBorderInfoProvider.BackgroundBrushSubscriptionDisposable { get; set; } = new();
#endif
}
#endif
