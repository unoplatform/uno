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
	internal override SKPath? Paint(in PaintingSession session)
	{
		var canvas = session.Canvas;

		if (Size.X == 0 || Size.Y == 0)
		{
			return null;
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

		return BuildOwnContentPath();
	}

	internal override bool RequiresRepaintOnEveryFrame =>
		_shapes?.OfType<CompositionSpriteShape>().Any(s => s.FillBrush?.RequiresRepaintOnEveryFrame ?? false) ?? false;

	internal override float DamageRegionSamplingMargin =>
		_shapes?.OfType<CompositionSpriteShape>().Select(s => s.FillBrush?.DamageRegionSamplingMargin ?? 0).DefaultIfEmpty(0f).Max() ?? 0;

	internal override bool CanPaint() => base.CanPaint() || (_shapes?.Any(s => s.CanPaint()) ?? false);

	internal override bool TryGetLocalContentBounds(out SKRect localBounds)
	{
		localBounds = default;

		if (_shapes is not { Count: > 0 } shapes)
		{
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
				return false;
			}
		}

		if (!any)
		{
			localBounds = SKRect.Empty;
			return true;
		}

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

		if (ShadowState is not null)
		{
			return TryGetShadowSilhouetteBounds(acc, out localBounds);
		}

		localBounds = acc;
		return true;
	}

	// Reused across repaints (one per visual): the damage consumer copies it, so rebuilding in place is safe
	// and avoids allocating a native path on every repaint.
	private SKPath? _ownContentPathBuffer;

	private SKPath? BuildOwnContentPath()
	{
		if (_shapes is not { Count: > 0 } shapes)
		{
			return null;
		}

		var dst = _ownContentPathBuffer ??= new SKPath();
		dst.Rewind();

		var any = false;
		for (var i = 0; i < shapes.Count; i++)
		{
			if (shapes[i] is CompositionSpriteShape sprite)
			{
				any |= sprite.GetRenderPath(dst);
			}
			else
			{
				return null;
			}
		}

		if (!any)
		{
			return null;
		}

		if (ViewBox is { } viewBox && viewBox.Size.X > 0 && viewBox.Size.Y > 0)
		{
			var sx = Size.X / viewBox.Size.X;
			var sy = Size.Y / viewBox.Size.Y;
			var m = SKMatrix.Concat(SKMatrix.CreateScale(sx, sy), SKMatrix.CreateTranslation(-viewBox.Offset.X, -viewBox.Offset.Y));
			dst.Transform(m);
		}

		return dst;
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
