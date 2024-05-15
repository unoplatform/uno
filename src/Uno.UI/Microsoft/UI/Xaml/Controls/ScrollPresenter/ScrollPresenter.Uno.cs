#if __SKIA__
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollPresenter : IBorderInfoProvider
{
	private BorderLayerRenderer _borderRenderer;

	partial void InitializePartial()
	{
		_borderRenderer = new BorderLayerRenderer(this);
	}

	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

	Brush IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => default;

	CornerRadius IBorderInfoProvider.CornerRadius => default;
}
#endif
