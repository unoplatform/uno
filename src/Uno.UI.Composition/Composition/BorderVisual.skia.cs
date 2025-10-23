#nullable enable

using System;
using System.Diagnostics;
using System.Numerics;
using Windows.Foundation;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

/// <summary>
/// A Visual that has a border and a background.
/// </summary>
internal class BorderVisual(Compositor compositor) : ContainerVisual(compositor)
{
	private static readonly SKPath _sparePrePaintingClippingPath = new SKPath();

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
	private SKRoundRect? _borderPathOuterRect;
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

	internal override void Paint(in PaintingSession session)
	{
		UpdatePathsAndCornerClip();

		if (_backgroundShape is { } backgroundShape)
		{
			session.Canvas.Save();
			// it's necessary to clip the background because not all backgrounds are simple rounded rectangles with a solid color.
			// E.g. effect brushes will draw outside the intended area if they're not clipped.
			_backgroundClip?.ApplyClip(this, session.Canvas);
			backgroundShape.Render(in session);
			session.Canvas.Restore();
		}

		base.Paint(in session);

		_borderShape?.Render(in session);
	}

	internal override bool GetPrePaintingClipping(SKPath dst)
	{
		// This method is only important for airspace (to accurately deal with corner radii, etc.),
		// other than that it doesn't really do anything.
		UpdatePathsAndCornerClip();

		if (_cornerRadius != CornerRadius.None && _borderPathOuterRect is { } rect)
		{
			if (base.GetPrePaintingClipping(dst))
			{
				var path = _sparePrePaintingClippingPath;

				path.Rewind();

				path.AddRoundRect(rect);
				dst.Op(path, SKPathOp.Intersect, dst);

				return true;
			}
			else
			{
				dst.Reset();
				dst.AddRoundRect(rect);
				return true;
			}
		}
		else
		{
			return base.GetPrePaintingClipping(dst);
		}
	}

	private protected override SKPath? GetPostPaintingClipping()
	{
		UpdatePathsAndCornerClip();
		return _childClipCausedByCornerRadius?.GetClipPath(this) is { } path
			? base.GetPostPaintingClipping() is { } baseClip
				? path.Op(baseClip, SKPathOp.Intersect)
				: path
			: base.GetPostPaintingClipping();
	}

	private protected override void ApplyPostPaintingClipping(SKCanvas canvas)
	{
		if (base.GetPostPaintingClipping() is null)
		{
			// At the time of writing, this branch is always taken
			UpdatePathsAndCornerClip();
			_childClipCausedByCornerRadius?.ApplyClip(this, canvas);
		}
		else if (GetPostPaintingClipping() is { } clip)
		{
			canvas.ClipPath(clip);
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

		var outerArea = new SKRect(0, 0, Size.X, Size.Y);
		var innerArea = new SKRect(
			(float)_borderThickness.Left,
			(float)_borderThickness.Top,
			(float)(_borderThickness.Left + Math.Max(0, Size.X - (_borderThickness.Left + _borderThickness.Right))),
			(float)(_borderThickness.Top + Math.Max(0, Size.Y - (_borderThickness.Top + _borderThickness.Bottom))));

		// note that we're sending (the full) Size, not size
		var fullCornerRadius = _cornerRadius.GetRadii(Size.ToSize(), _borderThickness);

		unsafe
		{
			var outerRadii = stackalloc SKPoint[4];
			var innerRadii = stackalloc SKPoint[4];
			fullCornerRadius.Outer.GetRadii(outerRadii);
			fullCornerRadius.Inner.GetRadii(innerRadii);

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

					var backgroundPath = CreateBackgroundPath(_useInnerBorderBoundsAsAreaForBackground, innerArea.Size,
						outerArea.Size, outerRadii, innerRadii);
					((CompositionPathGeometry)_backgroundShape!.Geometry!).Path =
						new CompositionPath(new SkiaGeometrySource2D(backgroundPath));
					_backgroundShape!.Offset = _useInnerBorderBoundsAsAreaForBackground
						? new Vector2((float)_borderThickness.Left, (float)_borderThickness.Top)
						: Vector2.Zero;
				}
				else if (_backgroundShape is not null) // reset values
				{
					((CompositionPathGeometry)_backgroundShape!.Geometry!).Path = null;
					_backgroundShape!.Offset = Vector2.Zero;
				}
			}

			if (!_borderPathValid)
			{
				_borderPathValid = true;
				if (_borderBrush is not null)
				{
					var borderPath = CreateBorderPath(innerArea, outerArea, outerRadii, innerRadii);
					((CompositionPathGeometry)_borderShape!.Geometry!).Path =
						new CompositionPath(new SkiaGeometrySource2D(borderPath));
				}
				else if (_borderShape is not null)
				{
					((CompositionPathGeometry)_borderShape!.Geometry!).Path = null;
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
				innerArea.Left, innerArea.Top, innerArea.Right, innerArea.Bottom,
				fullCornerRadius.Inner.TopLeft, fullCornerRadius.Inner.TopRight, fullCornerRadius.Inner.BottomRight, fullCornerRadius.Inner.BottomLeft);

			if (_useInnerBorderBoundsAsAreaForBackground)
			{
				_backgroundClip = Compositor.CreateRectangleClip(
					innerArea.Left, innerArea.Top, innerArea.Right, innerArea.Bottom,
					fullCornerRadius.Inner.TopLeft, fullCornerRadius.Inner.TopRight, fullCornerRadius.Inner.BottomRight, fullCornerRadius.Inner.BottomLeft);
			}
			else
			{
				_backgroundClip = Compositor.CreateRectangleClip(
					outerArea.Left, outerArea.Top, outerArea.Right, outerArea.Bottom,
					fullCornerRadius.Outer.TopLeft, fullCornerRadius.Outer.TopRight, fullCornerRadius.Outer.BottomRight, fullCornerRadius.Outer.BottomLeft);
			}
		}
	}

	private static unsafe SKPath CreateBackgroundPath(bool useInnerBorderBoundsAsAreaForBackground, SKSize innerArea, SKSize outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		var backgroundPath = new SKPath();
		var roundRect = new SKRoundRect();
		var rect = useInnerBorderBoundsAsAreaForBackground
			? new SKRect(0, 0, innerArea.Width, innerArea.Height)
			: new SKRect(0, 0, outerArea.Width, outerArea.Height);
		UnoSkiaApi.sk_rrect_set_rect_radii(
			roundRect.Handle,
			&rect,
			useInnerBorderBoundsAsAreaForBackground ? innerRadii : outerRadii);
		backgroundPath.AddRoundRect(roundRect);
		backgroundPath.Close();

		return backgroundPath;
	}

	private unsafe SKPath CreateBorderPath(SKRect innerArea, SKRect outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		var borderPath = new SKPath();

		borderPath.FillType = SKPathFillType.EvenOdd;

		// The order here (outer then inner) is important because of the SKPathFillType.
		{
			var outerRect = new SKRoundRect();
			UnoSkiaApi.sk_rrect_set_rect_radii(outerRect.Handle, &outerArea, outerRadii);
			_borderPathOuterRect = outerRect;
			borderPath.AddRoundRect(outerRect);
			borderPath.Close();
		}
		{
			var innerRect = new SKRoundRect();
			UnoSkiaApi.sk_rrect_set_rect_radii(innerRect.Handle, &innerArea, innerRadii);
			borderPath.AddRoundRect(innerRect);
			borderPath.Close();
		}

		return borderPath;
	}

	internal override bool CanPaint() =>
		(BackgroundBrush?.CanPaint() ?? false) ||
		(BorderBrush?.CanPaint() ?? false) ||
		base.CanPaint();

	internal override bool RequiresRepaintOnEveryFrame => (_backgroundBrush?.RequiresRepaintOnEveryFrame ?? false) || (_borderBrush?.RequiresRepaintOnEveryFrame ?? false);

	internal override bool HitTest(Point point)
	{
		UpdatePathsAndCornerClip();
		return (_borderShape?.HitTest(point) ?? false) || (_backgroundShape?.HitTest(point) ?? false);
	}
}
