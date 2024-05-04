#nullable  enable

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
	// we use this instead of 3 separate properties so that we don't have do the calculations in OnPropertyChangedCore
	// repeatedly if more than one of properties is changed (i.e. during the initial load).
	internal readonly record struct BorderStateWrapper(CornerRadius CornerRadius, Thickness Thickness, bool UseInnerBorderBoundsAsAreaForBackground);

	private BorderStateWrapper _borderShapeAndBackgroundState;
	private CompositionSpriteShape? _backgroundShape;
	private CompositionSpriteShape? _borderShape;
	private CompositionClip? _backgroundClip;

	public BorderStateWrapper BorderShapeAndBackgroundState
	{
		private get => _borderShapeAndBackgroundState;
		set => SetObjectProperty(ref _borderShapeAndBackgroundState, value);
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
			return _backgroundShape;
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
			return _borderShape;
		}
	}

	internal override void Paint(in PaintingSession session)
	{
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

		if (propertyName is not nameof(BorderShapeAndBackgroundState) && propertyName is not nameof(Size))
		{
			return;
		}

		// Note: To reduce allocations, we don't start with a fresh BorderShape and BackgroundShape, but
		// we modify their properties. This means that the properties might be outdated and we need to set
		// all relevant properties every time. Any state being set conditionally needs to be accompanied
		// by an else branch that resets the state to its default value (see CornerRadiusClip below).

		var (cornerRadius, borderThickness, useInnerBorderBoundsAsAreaForBackground) = BorderShapeAndBackgroundState;

		var outerArea = new SKRect(0, 0, Size.X, Size.Y);
		var innerArea = new SKRect(
			borderThickness.Left,
			borderThickness.Top,
			borderThickness.Left + Math.Max(0, Size.X - (borderThickness.Left + borderThickness.Right)),
			borderThickness.Top + Math.Max(0, Size.Y - (borderThickness.Top + borderThickness.Bottom)));

		// note that we're sending (the full) Size, not size
		var fullCornerRadius = cornerRadius.GetRadii(Size.ToSize(), borderThickness);

		unsafe
		{
			var outerRadii = stackalloc SKPoint[4];
			var innerRadii = stackalloc SKPoint[4];
			fullCornerRadius.Outer.GetRadii(outerRadii);
			fullCornerRadius.Inner.GetRadii(innerRadii);

			UpdateBackground(useInnerBorderBoundsAsAreaForBackground, innerArea, outerArea, outerRadii, innerRadii);
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
			CornerRadiusClip = Compositor.CreateRectangleClip(
				innerArea.Left, innerArea.Top, innerArea.Right, innerArea.Bottom,
				fullCornerRadius.Inner.TopLeft, fullCornerRadius.Inner.TopRight, fullCornerRadius.Inner.BottomRight, fullCornerRadius.Inner.BottomLeft);

			if (useInnerBorderBoundsAsAreaForBackground)
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
		else
		{
			CornerRadiusClip = null;
		}
	}

	private unsafe void UpdateBackground(bool useInnerBorderBoundsAsAreaForBackground, SKRect innerArea, SKRect outerArea, SKPoint* outerRadii, SKPoint* innerRadii)
	{
		var backgroundGeometry = (CompositionPathGeometry)(BackgroundShape.Geometry ??= Compositor.CreatePathGeometry());
		backgroundGeometry.Path ??= new CompositionPath(new SkiaGeometrySource2D());
		var backgroundPathGeometry = ((SkiaGeometrySource2D)backgroundGeometry.Path.GeometrySource).Geometry;
		backgroundPathGeometry.Reset();
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

		borderPathGeometry.Reset();

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
