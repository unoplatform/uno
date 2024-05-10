#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

/// <summary>
/// A ShapeVisual that has a border and a background.
/// </summary>
internal class BorderVisual(Compositor compositor) : ShapeVisual(compositor)
{
	// state received from BorderLayerRenderer
	private bool _borderStateValid;
	private CornerRadius _cornerRadius;
	private Thickness _borderThickness;
	private bool _useInnerBorderBoundsAsAreaForBackground;
	// State set and used inside the class
	private CompositionSpriteShape? _backgroundShape;
	private CompositionSpriteShape? _borderShape;
	private CompositionClip? _backgroundClip;
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

	// BackgroundShape and BorderShape are NOT added to this.Shapes, which both makes it easier
	// to reason about (no external tampering) and is also closer to what WinUI does.
	public CompositionSpriteShape BackgroundShape
	{
		get
		{
			if (_backgroundShape is null)
			{
				// we need this to track get notified on brush updates.
				SetProperty(ref _backgroundShape, Compositor.CreateSpriteShape());
#if DEBUG
				_backgroundShape!.Comment = "#borderBackground";
#endif
			}
			return _backgroundShape!;
		}
	}

	public CompositionSpriteShape BorderShape
	{
		get
		{
			if (_borderShape is null)
			{
				// we need this to track get notified on brush updates.
				SetProperty(ref _borderShape, Compositor.CreateSpriteShape());
#if DEBUG
				_borderShape!.Comment = "#borderShape";
#endif
			}
			return _borderShape!;
		}
	}

	private protected override void ApplyPostPaintingClipping(in SKCanvas canvas)
		=> _childClipCausedByCornerRadius?.Apply(canvas, this);

	internal override void Paint(in PaintingSession session)
	{
		UpdateBorderBackgroundShapes();
		if (_backgroundShape is { } backgroundShape)
		{
			session.Canvas.Save();
			// it's necessary to clip the background because not all backgrounds are simple rounded rectangles with a solid color.
			// E.g. effect brushes will draw outside the intended area if they're not clipped.
			_backgroundClip?.Apply(session.Canvas, this);
			backgroundShape.Render(in session);
			session.Canvas.Restore();
		}

		base.Paint(in session);

		_borderShape?.Render(in session);
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		// Call base implementation - Visual calls Compositor.InvalidateRender().
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

		if (propertyName is nameof(CornerRadius) or nameof(BorderThickness) or nameof(UseInnerBorderBoundsAsAreaForBackground) or nameof(Size))
		{
			_borderStateValid = false;
		}
	}

	private void UpdateBorderBackgroundShapes()
	{
		if (_borderStateValid)
		{
			return;
		}
		_borderStateValid = false;

		// clear old state
		_childClipCausedByCornerRadius = null;
		((_borderShape?.Geometry as CompositionPathGeometry)?.Path?.GeometrySource as SkiaGeometrySource2D)?.Geometry.Reset();
		((_backgroundShape?.Geometry as CompositionPathGeometry)?.Path?.GeometrySource as SkiaGeometrySource2D)?.Geometry.Reset();

		var outerArea = new SKRect(0, 0, Size.X, Size.Y);
		var innerArea = new SKRect(
			_borderThickness.Left,
			_borderThickness.Top,
			_borderThickness.Left + Math.Max(0, Size.X - (_borderThickness.Left + _borderThickness.Right)),
			_borderThickness.Top + Math.Max(0, Size.Y - (_borderThickness.Top + _borderThickness.Bottom)));

		// note that we're sending (the full) Size, not size
		var fullCornerRadius = _cornerRadius.GetRadii(Size.ToSize(), _borderThickness);

		unsafe
		{
			var outerRadii = stackalloc SKPoint[4];
			var innerRadii = stackalloc SKPoint[4];
			fullCornerRadius.Outer.GetRadii(outerRadii);
			fullCornerRadius.Inner.GetRadii(innerRadii);

			UpdateBackground(_useInnerBorderBoundsAsAreaForBackground, innerArea, outerArea, outerRadii, innerRadii);
			UpdateBorderShape(innerArea, outerArea, outerRadii, innerRadii);
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

	private unsafe void UpdateBackground(bool useInnerBorderBoundsAsAreaForBackground, SKRect innerArea, SKRect outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		var backgroundGeometry = (CompositionPathGeometry)(BackgroundShape.Geometry ??= Compositor.CreatePathGeometry());
		backgroundGeometry.Path ??= new CompositionPath(new SkiaGeometrySource2D());
		var backgroundPathGeometry = ((SkiaGeometrySource2D)backgroundGeometry.Path.GeometrySource).Geometry;
		var roundRect = new SKRoundRect();
		UnoSkiaApi.sk_rrect_set_rect_radii(
			roundRect.Handle,
			useInnerBorderBoundsAsAreaForBackground ? &innerArea : &outerArea,
			useInnerBorderBoundsAsAreaForBackground ? innerRadii : outerRadii);
		backgroundPathGeometry.AddRoundRect(roundRect);
		backgroundPathGeometry.Close();
	}

	private unsafe void UpdateBorderShape(SKRect innerArea, SKRect outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		var borderGeometry = (CompositionPathGeometry)(BorderShape.Geometry ??= Compositor.CreatePathGeometry());
		borderGeometry.Path ??= new CompositionPath(new SkiaGeometrySource2D(new SKPath()));
		var borderGeometrySource = (SkiaGeometrySource2D)borderGeometry.Path.GeometrySource;
		var borderPathGeometry = borderGeometrySource.Geometry;

		// It's important to set this every time, since borderPathGeometry.Reset will reset it.
		borderGeometrySource.Geometry.FillType = SKPathFillType.EvenOdd;

		// The order here (outer then inner) is important because of the SKPathFillType.
		{
			var outerRect = new SKRoundRect();
			UnoSkiaApi.sk_rrect_set_rect_radii(outerRect.Handle, &outerArea, outerRadii);
			borderPathGeometry.AddRoundRect(outerRect);
			borderPathGeometry.Close();
		}
		{
			var innerRect = new SKRoundRect();
			UnoSkiaApi.sk_rrect_set_rect_radii(innerRect.Handle, &innerArea, innerRadii);
			borderPathGeometry.AddRoundRect(innerRect);
			borderPathGeometry.Close();
		}
	}
}
