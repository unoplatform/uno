using System;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ScrollPresenter : IBorderInfoProvider, IScrollPortGeometry
{
	double IScrollPortGeometry.ZoomedExtentWidth => GetZoomedExtentWidth();

	double IScrollPortGeometry.ZoomedExtentHeight => GetZoomedExtentHeight();

	void IScrollPortGeometry.ScrollToOffsets(double horizontalOffset, double verticalOffset) => ScrollToOffsets(horizontalOffset, verticalOffset);

	internal InteractionTracker InteractionTracker => m_interactionTracker;

	private protected override ContainerVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();

	// WinUI's ScrollPresenter derives from Panel, so its (default Transparent) Background makes it hit-testable,
	// e.g. in the gaps between items.
	internal override bool IsViewHit() => Border.IsViewHitImpl(this);

	partial void OnBackgroundChangedPartial();

	Brush IBorderInfoProvider.Background => Background;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.InnerBorderEdge;

	Brush IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => default;

	CornerRadius IBorderInfoProvider.CornerRadius => default;

	BorderVisual IBorderInfoProvider.BorderVisual => Visual as BorderVisual ?? throw new InvalidCastException($"{nameof(IBorderInfoProvider)}s should use a {nameof(BorderVisual)}.");
}
