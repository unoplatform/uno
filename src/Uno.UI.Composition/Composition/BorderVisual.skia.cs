#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition;

/// <summary>
/// A Visual that has a border and a background.
/// </summary>
internal class BorderVisual(Compositor compositor) : ContainerVisual(compositor)
{
	// state set from outside and used inside the class
	private CornerRadius _cornerRadius;
	private Thickness _borderThickness;
	private bool _useInnerBorderBoundsAsAreaForBackground = true;
	private CompositionBrush? _backgroundBrush;
	private CompositionBrush? _borderBrush;
	// State set and used inside the class
	private bool _borderPathValid;
	private bool _backgroundPathValid;
	private CompositionSpriteShape? _backgroundShape; // Never null after _backgroundBrush is set
	private CompositionSpriteShape? _borderShape; // Never null after _borderBrush is set
	private CompositionClip? _backgroundClip;
	private RoundRectangle? _borderPathOuterRect;
	// state set here but affects children
	private RectangleClip? _childClipCausedByCornerRadius;

	// We do this instead of a direct SetProperty call so that SetProperty automatically gets an accurate propertyName
	// we need the SetProperty calls to get notified on brush updates.
	// (<Border|Background>Brush internals change -> <Border|Background>Shape is notified through FillBrush -> render invalidation)
	private CompositionSpriteShape? BackgroundShape { set => SetProperty(ref _backgroundShape, value); }
	private CompositionSpriteShape? BorderShape { set => SetProperty(ref _borderShape, value); }

	internal bool IsMyBackgroundShape(CompositionSpriteShape shape) => _backgroundShape == shape;

	public CornerRadius CornerRadius
	{
		private get => _cornerRadius;
		set => SetObjectProperty(ref _cornerRadius, value);
	}

	public Thickness BorderThickness
	{
		private get => _borderThickness;
		set => SetObjectProperty(ref _borderThickness, value);
	}

	public bool UseInnerBorderBoundsAsAreaForBackground
	{
		private get => _useInnerBorderBoundsAsAreaForBackground;
		set => SetProperty(ref _useInnerBorderBoundsAsAreaForBackground, value);
	}

	public CompositionBrush? BackgroundBrush
	{
		private get => _backgroundBrush;
		set => SetProperty(ref _backgroundBrush, value);
	}

	public CompositionBrush? BorderBrush
	{
		private get => _borderBrush;
		set => SetProperty(ref _borderBrush, value);
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		// Call base implementation - Visual calls Compositor.InvalidateRender().
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

		switch (propertyName)
		{
			case nameof(CornerRadius) or nameof(BorderThickness) or nameof(UseInnerBorderBoundsAsAreaForBackground) or nameof(Size):
				_borderPathValid = false;
				_backgroundPathValid = false;
				break;
			// BackgroundShape and BorderShape are NOT added to this.Shapes, which both makes it easier
			// to reason about (no external tampering) and is also closer to what WinUI does.
			case nameof(BorderBrush):
				_borderPathValid = false;
				if (BorderBrush is not null && _borderShape is null)
				{
					var borderShape = Compositor.CreateSpriteShape();
					borderShape.Geometry = Compositor.CreatePathGeometry();
#if DEBUG
					borderShape.Comment = "#borderShape";
#endif
					borderShape.FillBrush = BorderBrush;
					BorderShape = borderShape;
				}
				else if (_borderShape is { })
				{
					_borderShape.FillBrush = BorderBrush;
				}
				break;
			case nameof(BackgroundBrush):
				_backgroundPathValid = false;
				if (BackgroundBrush is not null && _backgroundShape is null)
				{
					var backgroundShape = Compositor.CreateSpriteShape();

					backgroundShape.Geometry = Compositor.CreatePathGeometry();
#if DEBUG
					backgroundShape.Comment = "#backgroundShape";
#endif
					backgroundShape.FillBrush = BackgroundBrush;

					BackgroundShape = backgroundShape;
				}
				else if (_backgroundShape is { })
				{
					_backgroundShape.FillBrush = BackgroundBrush;
				}
				break;
		}
	}

	internal override IGeometry? Paint(in PaintingSession session)
	{
		UpdatePathsAndCornerClip();

		if (_backgroundShape is { } backgroundShape)
		{
			session.Session.Save();
			// it's necessary to clip the background because not all backgrounds are simple rounded rectangles with a solid color.
			// E.g. effect brushes will draw outside the intended area if they're not clipped.
			_backgroundClip?.ApplyClip(this, session.Session);
			backgroundShape.Render(in session);
			session.Session.Restore();
		}

		base.Paint(in session);

		_borderShape?.Render(in session);

		// TODO(damage 1b): return BuildOwnContentPath() for precise border/background damage; bounds fallback for now.
		return null;
	}

	internal override void ApplyPrePaintingClipping(IDrawingSession session)
	{
		UpdatePathsAndCornerClip();
		base.ApplyPrePaintingClipping(session);
		if (_cornerRadius != CornerRadius.None && _borderPathOuterRect is { } rect)
		{
			session.ClipRoundRect(rect, antialias: true);
		}
	}

	internal override IGeometry? GetPrePaintingClipping()
	{
		// This method is only important for airspace (to accurately deal with corner radii, etc.),
		// other than that it doesn't really do anything.
		UpdatePathsAndCornerClip();

		var baseClip = base.GetPrePaintingClipping();
		if (_cornerRadius != CornerRadius.None && _borderPathOuterRect is { } rect)
		{
			var roundRect = BuildRoundRectGeometry(rect);
			return baseClip is null
				? roundRect
				: baseClip.Combine(roundRect, GeometryCombineMode.Intersect);
		}

		return baseClip;
	}

	private protected override IGeometry? GetPostPaintingClipping()
	{
		UpdatePathsAndCornerClip();
		return _childClipCausedByCornerRadius?.GetClipPath(this) is { } path
			? base.GetPostPaintingClipping() is { } baseClip
				? path.Combine(baseClip, GeometryCombineMode.Intersect)
				: path
			: base.GetPostPaintingClipping();
	}

	private protected override void ApplyPostPaintingClipping(IDrawingSession session)
	{
		if (base.GetPostPaintingClipping() is null)
		{
			// At the time of writing, this branch is always taken
			UpdatePathsAndCornerClip();
			_childClipCausedByCornerRadius?.ApplyClip(this, session);
		}
		else if (GetPostPaintingClipping() is { } clip)
		{
			session.ClipPath(clip, antialias: true);
		}
	}

	private void UpdatePathsAndCornerClip()
	{
		if (_borderPathValid && _backgroundPathValid)
		{
			return;
		}

		// clear old state
		_childClipCausedByCornerRadius = null;
		_backgroundClip = null;

		var borderLeft = (float)_borderThickness.Left;
		var borderTop = (float)_borderThickness.Top;
		var innerWidth = (float)Math.Max(0, Size.X - (_borderThickness.Left + _borderThickness.Right));
		var innerHeight = (float)Math.Max(0, Size.Y - (_borderThickness.Top + _borderThickness.Bottom));
		var outerArea = new Rect(0, 0, Size.X, Size.Y);
		var innerArea = new Rect(borderLeft, borderTop, innerWidth, innerHeight);

		// note that we're sending (the full) Size, not size
		var fullCornerRadius = _cornerRadius.GetRadii(Size.ToSize(), _borderThickness);

		{
			if (!_backgroundPathValid)
			{
				_backgroundPathValid = true;
				if (_backgroundBrush is not null)
				{
					// We don't pass down <inner|outer>Area directly, since it contains the thickness offsets.
					// Instead, we only pass the Size (without the X and Y offsets).
					// The offsets shouldn't be part of the background path calculations, but should be done
					// at the point of rendering by translation the final output by the thickness.
					// This matters because if the path is for an image with a scaling RelativeTransform.
					// In that case, if you factor the thickness in the path itself (i.e. include it in SKPath.Bounds),
					// the shader will sample from the image after the offset is applied.
					// E.g., if we have a border with a 20px border thickness and 100x100 background area for an ImageBrush with a
					// RelativeTransform = ScaleTransform { ScaleX = 3, ScaleY = 3, CenterX = 0.5, CenterY = 0.5 }, here's what we want:
					// |-----------------300px---------------------|
					// |                                           |
					// |<-100px->                        <-100px-> |
					// |         |---------100px--------|          |
					// |         |                      |<---------/---- what we want the shader to sample.
					// |         |      final           |          | <-- image scaled to 100*3 x 100*3
					// |         |      drawing         |          |
					// 300px   100px    area          100px      300px
					// |         |                      |          |
					// |         |                      |          |
					// |         |                      |          |
					// |         |---------100px--------|          |
					// |                                           |
					// |                                           |
					// |-----------------300px---------------------|

					// Here's what we don't want:
					//    |-----------------300px---------------------|
					//    |                                           |
					//    |<80px>                         <--120px--> |
					//    |      |---------100px--------|             |
					//    |      |                      |<------------/---- same exact final drawing area (in absolute window coordinates)
					//    |      |      final           |             | <-- but outer image shifted by 20px to the right
					//    |      |      drawing         |             |
					// 300px   100px    area          100px         300px
					//    |      |                      |             |
					//    |      |                      |             |
					//    |      |                      |             |
					//    |      |---------100px--------|             |
					//    |                                           |
					//    |                                           |
					//    |-----------------300px---------------------|

					var useInner = _useInnerBorderBoundsAsAreaForBackground;
					var bgRect = useInner ? new Rect(0, 0, innerWidth, innerHeight) : new Rect(0, 0, Size.X, Size.Y);
					var bgRadii = useInner ? fullCornerRadius.Inner : fullCornerRadius.Outer;
					var bgGeometry = BuildRoundRectPath(bgRect, bgRadii);
					((CompositionPathGeometry)_backgroundShape!.Geometry!).Path =
						new CompositionPath((IGeometrySource2D)bgGeometry);
					// Let a supporting backend fill the background as one analytic rounded rect (SDF) instead of the
					// tessellated path. The path stays set as the fallback (non-solid brushes, non-identity transforms).
					_backgroundShape!.RoundedRectFillHint = (bgRect, new Vector4(bgRadii.TopLeft.X, bgRadii.TopRight.X, bgRadii.BottomRight.X, bgRadii.BottomLeft.X));
					_backgroundShape!.Offset = useInner
						? new Vector2(borderLeft, borderTop)
						: Vector2.Zero;
				}
				else if (_backgroundShape is not null) // reset values
				{
					((CompositionPathGeometry)_backgroundShape!.Geometry!).Path = null;
					_backgroundShape!.RoundedRectFillHint = null;
					_backgroundShape!.Offset = Vector2.Zero;
				}
			}

			if (!_borderPathValid)
			{
				_borderPathValid = true;
				if (_borderBrush is not null)
				{
					_borderPathOuterRect = ToRoundRect(outerArea, fullCornerRadius.Outer);
					var borderGeometry = BuildRoundRectRingPath(outerArea, fullCornerRadius.Outer, innerArea, fullCornerRadius.Inner);
					((CompositionPathGeometry)_borderShape!.Geometry!).Path =
						new CompositionPath((IGeometrySource2D)borderGeometry);
					// Let a supporting backend fill the border as one analytic annulus (SDF) instead of a ring path.
					var or = fullCornerRadius.Outer; var ir = fullCornerRadius.Inner;
					_borderShape!.RoundedRectBorderHint = (
						outerArea, new Vector4(or.TopLeft.X, or.TopRight.X, or.BottomRight.X, or.BottomLeft.X),
						innerArea, new Vector4(ir.TopLeft.X, ir.TopRight.X, ir.BottomRight.X, ir.BottomLeft.X));
				}
				else if (_borderShape is not null)
				{
					((CompositionPathGeometry)_borderShape!.Geometry!).Path = null;
					_borderShape!.RoundedRectBorderHint = null;
				}
			}
		}

		// Note: The clipping is used to determine the location where the children of current element can be rendered.
		//		 So its has to be the "inner" area (i.e. the area without the border).
		//		 The border and the background shapes are already clipped properly and will be drawn without this clipping property set.
		// Note 2: This only applies when there is at least one corner with a corner radius. This means that a child
		//         that draws outside the bounds of this visual might not be clipped normally, but merely adding
		//         a non-empty CornerRadius will clip the child(ren). This matches WinUI even though it's not intuitive.
		if (!fullCornerRadius.IsEmpty)
		{
			_childClipCausedByCornerRadius = Compositor.CreateRectangleClip(
				(float)innerArea.Left, (float)innerArea.Top, (float)innerArea.Right, (float)innerArea.Bottom,
				fullCornerRadius.Inner.TopLeft, fullCornerRadius.Inner.TopRight, fullCornerRadius.Inner.BottomRight, fullCornerRadius.Inner.BottomLeft);

			if (_useInnerBorderBoundsAsAreaForBackground)
			{
				_backgroundClip = Compositor.CreateRectangleClip(
					(float)innerArea.Left, (float)innerArea.Top, (float)innerArea.Right, (float)innerArea.Bottom,
					fullCornerRadius.Inner.TopLeft, fullCornerRadius.Inner.TopRight, fullCornerRadius.Inner.BottomRight, fullCornerRadius.Inner.BottomLeft);
			}
			else
			{
				_backgroundClip = Compositor.CreateRectangleClip(
					(float)outerArea.Left, (float)outerArea.Top, (float)outerArea.Right, (float)outerArea.Bottom,
					fullCornerRadius.Outer.TopLeft, fullCornerRadius.Outer.TopRight, fullCornerRadius.Outer.BottomRight, fullCornerRadius.Outer.BottomLeft);
			}
		}
	}

	private static IGeometry BuildRoundRectGeometry(RoundRectangle roundRect)
	{
		var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
		builder.AddRoundedRectangle(roundRect.Rect, roundRect.TopLeft, roundRect.TopRight, roundRect.BottomRight, roundRect.BottomLeft);
		return builder.Build();
	}

	private static RoundRectangle ToRoundRect(Rect rect, NonUniformCornerRadius radii) => new()
	{
		Rect = rect,
		TopLeft = radii.TopLeft,
		TopRight = radii.TopRight,
		BottomRight = radii.BottomRight,
		BottomLeft = radii.BottomLeft,
	};

	internal override bool CanPaint() =>
		(BackgroundBrush?.CanPaint() ?? false) ||
		(BorderBrush?.CanPaint() ?? false) ||
		base.CanPaint();

	// The background fill and border ring both live inside (0, 0, Size.X, Size.Y) by construction.
	internal override bool PaintsWithinOwnSize => true;

	internal override bool RequiresRepaintOnEveryFrame => (_backgroundBrush?.RequiresRepaintOnEveryFrame ?? false) || (_borderBrush?.RequiresRepaintOnEveryFrame ?? false);

	internal override bool HitTest(Point point)
	{
		UpdatePathsAndCornerClip();
		return (_borderShape?.HitTest(point) ?? false) || (_backgroundShape?.HitTest(point) ?? false);
	}

	private protected override bool TryAddShadowPaths(List<(IGeometry path, float alpha)> output)
	{
		if (_backgroundBrush is null && _borderBrush is null)
		{
			return true;
		}

		// Only solid-color brushes are describable analytically — any non-color brush (gradient, image,
		// effect, surface) has a silhouette that can't be reduced to a finite tiling of constant-α paths.
		var backgroundColorBrush = _backgroundBrush as CompositionColorBrush;
		if (_backgroundBrush is not null && backgroundColorBrush is null)
		{
			return false;
		}
		var borderColorBrush = _borderBrush as CompositionColorBrush;
		if (_borderBrush is not null && borderColorBrush is null)
		{
			return false;
		}

		var backgroundAlpha = backgroundColorBrush?.Color.A ?? (byte)0;
		var borderAlpha = borderColorBrush?.Color.A ?? (byte)0;

		if (backgroundAlpha == 0 && borderAlpha == 0)
		{
			return true; // both transparent — visually empty
		}

		if (Size.X <= 0 || Size.Y <= 0)
		{
			return true;
		}

		UpdatePathsAndCornerClip();

		var outerArea = new Rect(0, 0, Size.X, Size.Y);
		var hasBorderThickness = _borderThickness.Left > 0
			|| _borderThickness.Top > 0
			|| _borderThickness.Right > 0
			|| _borderThickness.Bottom > 0;
		var hasBorder = borderAlpha > 0 && hasBorderThickness;
		var fullCornerRadius = _cornerRadius.GetRadii(Size.ToSize(), _borderThickness);

		if (!hasBorder)
		{
			// No border ring — only a background fill (possibly translucent).
			if (backgroundAlpha == 0)
			{
				return true;
			}

			var useInnerForBg = _useInnerBorderBoundsAsAreaForBackground && hasBorderThickness;
			var bgArea = useInnerForBg ? ComputeInnerArea(outerArea) : outerArea;
			var bgRadii = useInnerForBg ? fullCornerRadius.Inner : fullCornerRadius.Outer;
			output.Add((BuildRoundRectPath(bgArea, bgRadii), backgroundAlpha / 255f));
			return true;
		}

		// Has an opaque-enough border ring. Contribute the ring (outer ∖ inner) first, then optionally
		// the background. When alphas are equal the accumulator's MergeOpaque collapses them; when alphas
		// differ (translucent background through an opaque ring, or vice versa) the Porter-Duff `over`
		// math in the accumulator yields the correct multi-α tiling without us having to special-case it.
		var innerArea = ComputeInnerArea(outerArea);
		output.Add((BuildRoundRectRingPath(outerArea, fullCornerRadius.Outer, innerArea, fullCornerRadius.Inner), borderAlpha / 255f));

		if (backgroundAlpha > 0)
		{
			Rect bgArea;
			NonUniformCornerRadius bgRadii;
			if (_useInnerBorderBoundsAsAreaForBackground)
			{
				// Background covers the inner area only — no overlap with the ring.
				bgArea = innerArea;
				bgRadii = fullCornerRadius.Inner;
			}
			else
			{
				// Background covers the full outer area; the ring is drawn on top inside the outer.
				// Contributing the outer rect makes the accumulator compute α_ring `over` α_bg in the
				// overlap (the ring band) and just α_bg in the centre, which matches the rendered output.
				bgArea = outerArea;
				bgRadii = fullCornerRadius.Outer;
			}
			output.Add((BuildRoundRectPath(bgArea, bgRadii), backgroundAlpha / 255f));
		}

		return true;
	}

	private Rect ComputeInnerArea(Rect outerArea) => new(
		(float)_borderThickness.Left,
		(float)_borderThickness.Top,
		Math.Max(0, outerArea.Right - _borderThickness.Right - _borderThickness.Left),
		Math.Max(0, outerArea.Bottom - _borderThickness.Bottom - _borderThickness.Top));

	private static IGeometry BuildRoundRectPath(Rect rect, NonUniformCornerRadius radii)
	{
		var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
		if (radii.IsEmpty)
		{
			builder.AddRectangle(rect);
		}
		else
		{
			builder.AddRoundedRectangle(rect, radii.TopLeft, radii.TopRight, radii.BottomRight, radii.BottomLeft);
		}
		return builder.Build();
	}

	private static IGeometry BuildRoundRectRingPath(
		Rect outerRect,
		NonUniformCornerRadius outerRadii,
		Rect innerRect,
		NonUniformCornerRadius innerRadii)
	{
		// EvenOdd fill across the outer and inner contours yields the ring region (outer ∖ inner).
		var builder = GeometryFactory.Current.CreatePrimitiveGeometryBuilder();
		builder.FillRule = GeometryFillRule.EvenOdd;

		if (outerRadii.IsEmpty)
		{
			builder.AddRectangle(outerRect);
		}
		else
		{
			builder.AddRoundedRectangle(outerRect, outerRadii.TopLeft, outerRadii.TopRight, outerRadii.BottomRight, outerRadii.BottomLeft);
		}

		if (!innerRect.IsEmpty)
		{
			if (innerRadii.IsEmpty)
			{
				builder.AddRectangle(innerRect);
			}
			else
			{
				builder.AddRoundedRectangle(innerRect, innerRadii.TopLeft, innerRadii.TopRight, innerRadii.BottomRight, innerRadii.BottomLeft);
			}
		}

		return builder.Build();
	}
}
