#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Windows.UI.Composition;

/// <summary>
/// A ShapeVisual that has a border and a background.
/// </summary>
internal class BorderVisual(Compositor compositor) : ShapeVisual(compositor)
{
	[ThreadStatic] // this should be tied to the compositor
	private static CompositionPathGeometry? _sharedPathGeometry;

	// state received from BorderLayerRenderer
	private CornerRadius _cornerRadius;
	private Thickness _borderThickness;
	private bool _useInnerBorderBoundsAsAreaForBackground = true;
	private CompositionBrush? _backgroundBrush;
	private CompositionBrush? _borderBrush;
	// State set and used inside the class
	private bool _borderPathValid;
	private bool _backgroundPathValid;
	private CompositionSpriteShape? _backgroundShape;
	private CompositionSpriteShape? _borderShape;
	private CompositionClip? _backgroundClip;
	private SKPath? _borderPath;
	private SKPath? _backgroundPath;
	// state set here but affects children
	private RectangleClip? _childClipCausedByCornerRadius;

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
				_borderPathValid = false; // to update _borderPath if previously skipped
				if (BorderBrush is not null && _borderShape is null)
				{
					// we need this to track get notified on brush updates.
					SetProperty(ref _borderShape, Compositor.CreateSpriteShape());
#if DEBUG
					_borderShape!.Comment = "#borderShape";
#endif
				}
				if (_borderShape is { })
				{
					_borderShape.FillBrush = BorderBrush;
				}
				break;
			case nameof(BackgroundBrush):
				_backgroundPathValid = false; // to update _backgroundPath if previously skipped
				if (BackgroundBrush is not null && _backgroundShape is null)
				{
					// we need this to track get notified on brush updates.
					SetProperty(ref _backgroundShape, Compositor.CreateSpriteShape());
#if DEBUG
					_backgroundShape!.Comment = "#backgroundShape";
#endif
				}
				if (_backgroundShape is { })
				{
					_backgroundShape.FillBrush = BackgroundBrush;
				}
				break;
		}
	}

	internal override void Paint(in PaintingSession session)
	{
		_sharedPathGeometry ??= Compositor.CreatePathGeometry();
		_sharedPathGeometry.Path ??= new CompositionPath(new SkiaGeometrySource2D());
		var geometrySource = (SkiaGeometrySource2D)_sharedPathGeometry.Path.GeometrySource;

		UpdatePathsAndCornerClip();

		if (_backgroundShape is { } backgroundShape && _backgroundPath is { } backgroundPath)
		{
			backgroundShape.Geometry = _sharedPathGeometry; // will only do something the first time
			geometrySource.Geometry = backgroundPath; // changing Geometry doesn't raise OnPropertyChanged or invalidate render.
			session.Canvas.Save();
			// it's necessary to clip the background because not all backgrounds are simple rounded rectangles with a solid color.
			// E.g. effect brushes will draw outside the intended area if they're not clipped.
			_backgroundClip?.Apply(session.Canvas, this);
			backgroundShape.Render(in session);
			session.Canvas.Restore();
		}

		base.Paint(in session);

		if (_borderShape is { } borderShape && _borderPath is { } borderPath)
		{
			borderShape.Geometry = _sharedPathGeometry; // will only do something the first time
			geometrySource.Geometry = borderPath; // changing Geometry doesn't raise OnPropertyChanged or invalidate render.
			_borderShape?.Render(in session);
		}
	}

	private protected override void ApplyPostPaintingClipping(in SKCanvas canvas)
		=> _childClipCausedByCornerRadius?.Apply(canvas, this);

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

			if (_backgroundBrush is { } && !_backgroundPathValid)
			{
				_backgroundPathValid = true;
				UpdateBackgroundPath(_useInnerBorderBoundsAsAreaForBackground, innerArea, outerArea, outerRadii, innerRadii);
			}
			if (_borderBrush is { } && !_borderPathValid)
			{
				_borderPathValid = true;
				UpdateBorderPath(innerArea, outerArea, outerRadii, innerRadii);
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

	private unsafe void UpdateBackgroundPath(bool useInnerBorderBoundsAsAreaForBackground, SKRect innerArea, SKRect outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		_backgroundPath ??= new SKPath();
		_backgroundPath.Reset();
		var roundRect = new SKRoundRect();
		UnoSkiaApi.sk_rrect_set_rect_radii(
			roundRect.Handle,
			useInnerBorderBoundsAsAreaForBackground ? &innerArea : &outerArea,
			useInnerBorderBoundsAsAreaForBackground ? innerRadii : outerRadii);
		_backgroundPath.AddRoundRect(roundRect);
		_backgroundPath.Close();
	}

	private unsafe void UpdateBorderPath(SKRect innerArea, SKRect outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		_borderPath ??= new SKPath();
		_borderPath.Reset();

		// It's important to set this every time, since borderPathGeometry.Reset will reset it.
		_borderPath.FillType = SKPathFillType.EvenOdd;

		// The order here (outer then inner) is important because of the SKPathFillType.
		{
			var outerRect = new SKRoundRect();
			UnoSkiaApi.sk_rrect_set_rect_radii(outerRect.Handle, &outerArea, outerRadii);
			_borderPath.AddRoundRect(outerRect);
			_borderPath.Close();
		}
		{
			var innerRect = new SKRoundRect();
			UnoSkiaApi.sk_rrect_set_rect_radii(innerRect.Handle, &innerArea, innerRadii);
			_borderPath.AddRoundRect(innerRect);
			_borderPath.Close();
		}
	}
}
