#nullable enable

using Uno.UI.Composition.Drawing;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Uno.Extensions;
using Uno.UI.Composition;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

public partial class ShapeVisual
{
	/// <inheritdoc />
	internal override void Paint(in PaintingSession session)
	{
		if (Size.X == 0 || Size.Y == 0)
		{
			return;
		}

		// TODO: ShapeVisuals should be clipping to the size rect. However, this breaks shapes for us because
		// we implement them with ShapeVisuals and they don't clip anything. The problem is that
		// the WinUI implementation doesn't use ShapeVisuals for shapes, but a combination of ContainerVisuals and
		// SpriteVisuals. When_StrokeThickness_Is_GreaterThan_Or_Equals_Width and
		// When_Border_CornerRadius_HitTesting fail when you uncomment the following line.
		// session.Session.ClipRect(new Rect(0, 0, Size.X, Size.Y));

		// TODO: ViewBox.Stretch, ViewBox.HorizontalAlignmentRatio and ViewBox.VerticalAlignmentRatio
		// Scoped to the shapes: repainting every frame paints straight into the session the children draw in next.
		var viewBoxMatrix = GetViewBoxMatrix();
		if (viewBoxMatrix is { } m)
		{
			session.Session.Save();
			session.Session.Concat(new Matrix4x4(m));
		}

		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				shapes[i].Render(in session);
			}
		}

		if (viewBoxMatrix is not null)
		{
			session.Session.Restore();
		}

		base.Paint(in session);
	}

	// Queried once per shape element per frame, so these walk the collection by index: the LINQ chains
	// they replace allocated an OfType iterator, a Select iterator and a closure on every frame.
	internal override bool RequiresRepaintOnEveryFrame
	{
		get
		{
			if (_shapes is not { Count: not 0 } shapes)
			{
				return false;
			}

			for (var i = 0; i < shapes.Count; i++)
			{
				if (shapes[i] is CompositionSpriteShape { FillBrush: { } fill } && fill.RequiresRepaintOnEveryFrame)
				{
					return true;
				}
			}

			return false;
		}
	}

	internal override float DamageRegionSamplingMargin
	{
		get
		{
			if (_shapes is not { Count: not 0 } shapes)
			{
				return 0;
			}

			// `seen` keeps the DefaultIfEmpty(0f) semantics: 0 only when there is no sprite shape at all,
			// rather than clamping a set of margins up to 0.
			var max = 0f;
			var seen = false;
			for (var i = 0; i < shapes.Count; i++)
			{
				if (shapes[i] is not CompositionSpriteShape sprite)
				{
					continue;
				}

				var margin = sprite.FillBrush?.DamageRegionSamplingMargin ?? 0;
				if (!seen || margin > max)
				{
					max = margin;
					seen = true;
				}
			}

			return seen ? max : 0;
		}
	}

	internal override bool CanPaint()
	{
		if (base.CanPaint())
		{
			return true;
		}

		if (_shapes is { Count: not 0 } shapes)
		{
			for (var i = 0; i < shapes.Count; i++)
			{
				if (shapes[i].CanPaint())
				{
					return true;
				}
			}
		}

		return false;
	}

	internal override bool TryGetLocalContentBounds(out Rect localBounds)
	{
		localBounds = default;

		if (_shapes is not { Count: > 0 } shapes)
		{
			return ShadowState is null;
		}

		var any = false;
		var acc = default(Rect);
		for (var i = 0; i < shapes.Count; i++)
		{
			// A shape this doesn't know how to bound leaves the whole visual unbounded.
			if (shapes[i] is not CompositionSpriteShape sprite)
			{
				return false;
			}

			if (sprite.TryGetRenderBounds(out var shapeBounds))
			{
				if (any)
				{
					acc.Union(shapeBounds);
				}
				else
				{
					acc = shapeBounds;
					any = true;
				}
			}
		}

		if (!any)
		{
			return true;
		}

		if (GetViewBoxMatrix() is { } viewBoxMatrix)
		{
			acc = acc.Transform(viewBoxMatrix);
		}

		if (ShadowState is not null)
		{
			return TryGetShadowSilhouetteBounds(acc, out localBounds);
		}

		localBounds = acc;
		return true;
	}

	private protected override bool TryAddShadowPaths(global::System.Collections.Generic.List<(IGeometry path, float alpha)> output)
	{
		if (_shapes is not { Count: > 0 } shapes || Size.X == 0 || Size.Y == 0)
		{
			return true;
		}

		foreach (var shape in shapes)
		{
			if (shape is not CompositionSpriteShape sprite)
			{
				return false;
			}
			if (!TryGetShadowBrushAlpha(sprite.FillBrush, out var fillAlpha) || !TryGetShadowBrushAlpha(sprite.StrokeBrush, out var strokeAlpha))
			{
				return false;
			}
			var hasFill = sprite.FillBrush is not null;
			var hasStroke = sprite.StrokeBrush is not null && sprite.StrokeThickness > 0;
			if (!hasFill && !hasStroke)
			{
				continue;
			}
			// BuildRenderGeometry unions the filled area and the stroke band, so the shape is describable as
			// one constant-α path only when every painted part carries the same alpha.
			if (hasFill && hasStroke && fillAlpha != strokeAlpha)
			{
				return false;
			}
			var alpha = hasFill ? fillAlpha : strokeAlpha;
			if (alpha <= 0)
			{
				continue;
			}
			if (BuildShapeGeometryInVisual(sprite, paintingBrushesOnly: false) is not { } geometry)
			{
				continue;
			}
			output.Add((geometry, alpha));
		}

		return true;
	}

	// Past these, the boolean ops cost more than cutting the whole clip out of a native element is worth.
	private const int MaxPaintedGeometryShapes = 16;
	private const int MaxPaintedGeometrySegments = 1024;

	// The last TryGetPaintedGeometry answer, kept until the next paint invalidation: it is asked every frame.
	private bool _hasPaintedGeometry;
	private bool _isPaintedGeometryBounded;
	private IGeometry? _paintedGeometry;

	internal override bool TryGetPaintedGeometry(out IGeometry? painted)
	{
		if (!_hasPaintedGeometry)
		{
			_isPaintedGeometryBounded = TryBuildPaintedGeometry(out _paintedGeometry);
			_hasPaintedGeometry = true;
		}

		painted = _paintedGeometry;
		painted?.AddRef();
		return _isPaintedGeometryBounded;
	}

	private protected override void OnPaintInvalidated() => ReleasePaintedGeometry();

	private protected override void DisposeInternal()
	{
		ReleasePaintedGeometry();
		base.DisposeInternal();
	}

	private void ReleasePaintedGeometry()
	{
		_paintedGeometry?.Release();
		_paintedGeometry = null;
		_hasPaintedGeometry = false;
	}

	private bool TryBuildPaintedGeometry(out IGeometry? painted)
	{
		painted = null;
		if (_shapes is not { Count: > 0 } shapes || Size.X == 0 || Size.Y == 0)
		{
			return true;
		}

		if (shapes.Count > MaxPaintedGeometryShapes)
		{
			return false;
		}

		for (var i = 0; i < shapes.Count; i++)
		{
			if (shapes[i] is not CompositionSpriteShape sprite)
			{
				painted?.Release();
				painted = null;
				return false;
			}

			if (BuildShapeGeometryInVisual(sprite, paintingBrushesOnly: true) is not { } geometry)
			{
				continue;
			}

			if (painted is null)
			{
				painted = geometry;
			}
			else
			{
				var previous = painted;
				painted = previous.Combine(geometry, GeometryCombineMode.Union);
				previous.Release();
				geometry.Release();
			}

			if (painted.SegmentCount > MaxPaintedGeometrySegments)
			{
				painted.Release();
				painted = null;
				return false;
			}
		}

		return true;
	}

	// What a sprite shape paints, in this visual's coordinates: its own transform (an Offset or rotation, typical of
	// LottieGen output), then the ViewBox, as CompositionShape.Render and Paint apply them.
	private IGeometry? BuildShapeGeometryInVisual(CompositionSpriteShape sprite, bool paintingBrushesOnly)
	{
		if (sprite.BuildRenderGeometry(paintingBrushesOnly) is not { } geometry)
		{
			return null;
		}

		var transform = sprite.GetRenderTransform();
		if (GetViewBoxMatrix() is { } viewBoxMatrix)
		{
			transform *= viewBoxMatrix;
		}

		if (transform.IsIdentity)
		{
			return geometry;
		}

		var transformed = geometry.Transform(transform);
		geometry.Release();
		return transformed;
	}

	// The ViewBox maps its Offset to the origin, then scales its Size to this visual's (an empty axis isn't scaled).
	private Matrix3x2? GetViewBoxMatrix()
		=> ViewBox is { } viewBox
			? Matrix3x2.CreateTranslation(-viewBox.Offset.X, -viewBox.Offset.Y)
				* Matrix3x2.CreateScale(
					viewBox.Size.X > 0 ? Size.X / viewBox.Size.X : 1,
					viewBox.Size.Y > 0 ? Size.Y / viewBox.Size.Y : 1)
			: null;

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
