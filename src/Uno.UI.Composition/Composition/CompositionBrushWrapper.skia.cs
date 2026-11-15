using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

internal partial class CompositionBrushWrapper : CompositionBrush
{
	private CompositionBrush _wrappedBrush;

	internal CompositionBrush WrappedBrush
	{
		get => _wrappedBrush;
		set => SetProperty(ref _wrappedBrush, value);
	}

	internal CompositionBrushWrapper(CompositionBrush wrappedBrush, Compositor compositor) : base(compositor)
	{
		WrappedBrush = wrappedBrush;
	}

	internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds) => _wrappedBrush?.TryPaint(session, opacity, bounds) ?? true;
	internal override bool CanPaint() => WrappedBrush?.CanPaint() ?? false;

	// Every XamlCompositionBrushBase-backed brush is wrapped, so without this an effect-graph or nine-grid source
	// would rasterize its own offscreen nested inside the caller's — exactly what the hook exists to prevent.
	internal override void PrepareForOffscreenRasterization(IDrawingFactory factory, Rect bounds, Vector2 scale)
		=> _wrappedBrush?.PrepareForOffscreenRasterization(factory, bounds, scale);

	internal override bool RequiresRepaintOnEveryFrame => WrappedBrush?.RequiresRepaintOnEveryFrame ?? false;
	internal override float DamageRegionSamplingMargin => WrappedBrush?.DamageRegionSamplingMargin ?? 0;
}
