using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollPresenter : IBorderInfoProvider
{
#if !UNO_HAS_BORDER_VISUAL
	private BorderLayerRenderer _borderRenderer;
#endif

#if !UNO_HAS_BORDER_VISUAL
	partial void InitializePartial()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}
#endif

#if UNO_HAS_BORDER_VISUAL
	private protected override ContainerVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();
#endif

	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

	Brush IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => default;

	CornerRadius IBorderInfoProvider.CornerRadius => default;

#if UNO_HAS_BORDER_VISUAL
	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");
#endif
}
