#nullable enable

using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using SkiaSharp;
using Uno.UI.Composition;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{

	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		var canvas = session.Canvas;

		if (Size.X == 0 || Size.Y == 0)
		{
			return;
		}

		// TODO: ShapeVisuals should be clipping to the size rect. However, this breaks shapes for us because
		// we implement them with ShapeVisuals and they don't clip anything. The problem is that
		// the WinUI implementation doesn't use ShapeVisuals for shapes, but a combination of ContainerVisuals and
		// SpriteVisuals. When_StrokeThickness_Is_GreaterThan_Or_Equals_Width and
		// When_Border_CornerRadius_HitTesting fail when you uncomment the following line.
		// canvas.ClipRect(new SKRect(0, 0, Size.X, Size.Y));

		// TODO: ViewBox.Stretch, ViewBox.HorizontalAlignmentRatio and ViewBox.VerticalAlignmentRatio
		if (ViewBox is not null)
		{
			canvas.Scale(
				ViewBox.Size.X > 0 ? Size.X / ViewBox.Size.X : 1,
				ViewBox.Size.Y > 0 ? Size.Y / ViewBox.Size.Y : 1);
			canvas.Translate(-ViewBox.Offset.X, -ViewBox.Offset.Y); // translate before scaling
		}

		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}

		base.Paint(in session);
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
	}

	// Evaluated live (not cached): a fill brush's RequiresRepaintOnEveryFrame can flip to true after the
	// shape is first assigned — e.g. a backdrop effect brush only reports it once it has detected its
	// backdrop input during its first paint. A stale cached value would freeze the visual's picture and,
	// under dirty rectangles, leave the backdrop effect rendering against a clip-starved backdrop.
	internal override bool RequiresRepaintOnEveryFrame =>
		_shapes?.OfType<CompositionSpriteShape>().Any(s => s.FillBrush?.RequiresRepaintOnEveryFrame ?? false) ?? false;

	internal override float DirtyRegionSamplingMargin =>
		_shapes?.OfType<CompositionSpriteShape>().Select(s => s.FillBrush?.DirtyRegionSamplingMargin ?? 0).DefaultIfEmpty(0f).Max() ?? 0;

	internal override bool CanPaint() => base.CanPaint() || (_shapes?.Any(s => s.CanPaint()) ?? false);

	// A ShapeVisual paints arbitrary geometry that can extend past its Size (strokes, paths with their own
	// coordinate range), so Size is not a valid bound. Use the union of the shapes' render bounds instead,
	// mapped through the same ViewBox transform Paint applies. Falls back to the clip (returns false) for
	// shapes whose bounds we can't compute yet, or when a drop shadow paints beyond the shapes.
	internal override bool TryGetLocalContentBounds(out SKRect localBounds)
	{
		localBounds = default;

		if (_shapes is not { Count: > 0 } shapes)
		{
			// No shapes: if there's a shadow it has no silhouette here; otherwise nothing is painted.
			if (ShadowState is not null)
			{
				return false;
			}
			localBounds = SKRect.Empty;
			return true;
		}

		var any = false;
		var acc = SKRect.Empty;
		for (var i = 0; i < shapes.Count; i++)
		{
			if (shapes[i] is CompositionSpriteShape sprite)
			{
				if (sprite.TryGetRenderBounds(out var shapeBounds))
				{
					acc = any ? SKRect.Union(acc, shapeBounds) : shapeBounds;
					any = true;
				}
			}
			else
			{
				// A non-sprite shape (e.g. a container shape) we can't bound; use the clip.
				return false;
			}
		}

		if (!any)
		{
			localBounds = SKRect.Empty;
			return true;
		}

		// Mirror the ViewBox transform applied to the canvas in Paint (Scale, then Translate(-Offset)).
		if (ViewBox is { } viewBox && viewBox.Size.X > 0 && viewBox.Size.Y > 0)
		{
			var sx = Size.X / viewBox.Size.X;
			var sy = Size.Y / viewBox.Size.Y;
			acc = new SKRect(
				sx * (acc.Left - viewBox.Offset.X),
				sy * (acc.Top - viewBox.Offset.Y),
				sx * (acc.Right - viewBox.Offset.X),
				sy * (acc.Bottom - viewBox.Offset.Y));
		}

		localBounds = ExpandForShadow(acc);
		return true;
	}

	/// <remarks>This does NOT take the clipping into account.</remarks>
	internal override bool HitTest(Point point)
	{
		if (_shapes is null)
		{
			return false;
		}

		foreach (var shape in _shapes)
		{
			if (shape.HitTest(point))
			{
				return true;
			}
		}

		// Do not check the child visuals. On WinUI, if you add a child visual (e.g. using ContainerVisual.Children.InsertAtTop),
		// the child doesn't factor at all in hit-testing. The children of the UIElement that owns this visual will be checked
		// separately in VisualTreeHelper.HitTest

		return false;
	}
}
