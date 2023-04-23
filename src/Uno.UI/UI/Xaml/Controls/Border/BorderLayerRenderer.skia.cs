using System;
using System.Collections.Generic;
using System.Numerics;
using SkiaSharp;
using Uno.Disposables;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class BorderLayerRenderer
{
	private readonly static SKPoint[] _outerRadiiStore = new SKPoint[4];
	private readonly static SKPoint[] _innerRadiiStore = new SKPoint[4];

	private readonly SerialDisposable _layerDisposable = new();

	/// <summary>
	/// Updates or creates a sublayer to render a border-like shape.
	/// </summary>
	partial void UpdateLayer()
	{
		// Bounds is captured to avoid calling twice calls below.
		var area = new Rect(0, 0, _owner.ActualWidth, _owner.ActualHeight);

		var newState = new BorderLayerState(area, _borderInfoProvider);

		var previousLayoutState = _currentState;

		if (!newState.Equals(previousLayoutState))
		{
			_layerDisposable.Disposable = null;

			if (_borderInfoProvider.Background != null ||
				_borderInfoProvider.CornerRadius != CornerRadius.None ||
				(_borderInfoProvider.BorderThickness != Thickness.Empty && _borderInfoProvider.BorderBrush != null))
			{
				_layerDisposable.Disposable = InnerCreateLayer(_owner, newState);
			}

			_currentState = newState;
		}
	}

	partial void ClearLayer()
	{
		_layerDisposable.Disposable = null;
		_currentState = default;
	}

	private static IDisposable InnerCreateLayer(UIElement owner, BorderLayerState state)
	{
		var area = owner.LayoutRound(state.Area);

		// In case the element has no size, skip everything!
		if (area.Width == 0 && area.Height == 0)
		{
			return Disposable.Empty;
		}

		var parent = owner.Visual;
		var compositor = parent.Compositor;
		var background = state.Background;
		var borderThickness = owner.LayoutRound(state.BorderThickness);
		var borderBrush = state.BorderBrush;
		var cornerRadius = state.CornerRadius;

		var disposables = new CompositeDisposable();
		var sublayers = new List<Visual>();

		var heightOffset = ((float)borderThickness.Top / 2) + ((float)borderThickness.Bottom / 2);
		var widthOffset = ((float)borderThickness.Left / 2) + ((float)borderThickness.Right / 2);
		var halfWidth = (float)area.Width / 2;
		var halfHeight = (float)area.Height / 2;
		var adjustedArea = state.BackgroundSizing == BackgroundSizing.InnerBorderEdge
			? area.DeflateBy(borderThickness)
			: area;

		var fullCornerRadius = cornerRadius.GetRadii(area.Size, borderThickness);

		if (!fullCornerRadius.IsEmpty)
		{
			var borderShape = compositor.CreateSpriteShape();
			var backgroundShape = compositor.CreateSpriteShape();
			var outerShape = compositor.CreateSpriteShape();
			var clipShape = compositor.CreateSpriteShape();

			// Border brush
			Brush.AssignAndObserveBrush(borderBrush, compositor, brush => borderShape.FillBrush = brush)
					.DisposeWith(disposables);

			// Background brush
			if (background is ImageBrush imgBackground)
			{
				adjustedArea = CreateImageLayer(compositor, disposables, borderThickness, adjustedArea, backgroundShape, adjustedArea, imgBackground);
			}
			else
			{
				Brush.AssignAndObserveBrush(background, compositor, brush => backgroundShape.FillBrush = brush)
					.DisposeWith(disposables);
			}

			// This needs to be adjusted if multiple UI threads are used in the future for multi-window
			fullCornerRadius.Outer.GetRadii(_outerRadiiStore);
			fullCornerRadius.Inner.GetRadii(_innerRadiiStore);

			// Background shape
			var backgroundPath = state.BackgroundSizing == BackgroundSizing.InnerBorderEdge ?
				GetRoundedPath(adjustedArea.ToSKRect(), _innerRadiiStore) :
				GetRoundedPath(area.ToSKRect(), _outerRadiiStore);
			backgroundShape.Geometry = compositor.CreatePathGeometry(backgroundPath);
			var backgroundVisual = compositor.CreateShapeVisual();
			backgroundVisual.Shapes.Add(backgroundShape);
			sublayers.Add(backgroundVisual);
			parent.Children.InsertAtBottom(backgroundVisual);

			// Border shape (if any)
			if (borderThickness != Thickness.Empty)
			{
				var borderPath = GetBorderPath(_outerRadiiStore, _innerRadiiStore, area, adjustedArea);
				borderShape.Geometry = compositor.CreatePathGeometry(borderPath);

				var borderVisual = compositor.CreateShapeVisual();

				borderVisual.Shapes.Add(borderShape);
				sublayers.Add(borderVisual);

				parent.Children.InsertAtTop(borderVisual);
			}

			owner.ClippingIsSetByCornerRadius = true;
			parent.Clip = compositor.CreateRectangleClip(
				0, 0, (float)area.Width, (float)area.Height,
				fullCornerRadius.Outer.TopLeft, fullCornerRadius.Outer.TopRight, fullCornerRadius.Outer.BottomRight, fullCornerRadius.Outer.BottomLeft);
		}
		else
		{
			var shapeVisual = compositor.CreateShapeVisual();

			var backgroundShape = compositor.CreateSpriteShape();

			var backgroundArea = area;

			// Background brush
			if (background is ImageBrush imgBackground)
			{
				backgroundArea = CreateImageLayer(compositor, disposables, borderThickness, adjustedArea, backgroundShape, backgroundArea, imgBackground);
			}
			else
			{
				Brush.AssignAndObserveBrush(background, compositor, brush => backgroundShape.FillBrush = brush)
					.DisposeWith(disposables);

				// This is required because changing the CornerRadius changes the background drawing
				// implementation and we don't want a rectangular background behind a rounded background.
				Disposable.Create(() => backgroundShape.FillBrush = null)
					.DisposeWith(disposables);
			}

			var geometrySource = new SkiaGeometrySource2D();
			var geometry = geometrySource.Geometry;

			geometry.AddRect(adjustedArea.ToSKRect());

			backgroundShape.Geometry = compositor.CreatePathGeometry(new CompositionPath(geometrySource));

			shapeVisual.Shapes.Add(backgroundShape);

			// Border shape (if any)
			if (borderThickness != Thickness.Empty)
			{
				Action<Action<CompositionSpriteShape, SKPath>> createLayer = builder =>
				{
					var spriteShape = compositor.CreateSpriteShape();
					var geometry = new SkiaGeometrySource2D();

					// Border brush
					Brush.AssignAndObserveBrush(borderBrush, compositor, brush => spriteShape.StrokeBrush = brush)
							.DisposeWith(disposables);

					builder(spriteShape, geometry.Geometry);
					spriteShape.Geometry = compositor.CreatePathGeometry(new CompositionPath(geometry));

					shapeVisual.Shapes.Add(spriteShape);
				};

				if (borderThickness.Top != 0)
				{
					createLayer((l, path) =>
					{
						l.StrokeThickness = (float)borderThickness.Top;
						var StrokeThicknessAdjust = (float)(borderThickness.Top / 2);
						path.MoveTo((float)(area.X + borderThickness.Left), (float)(area.Y + StrokeThicknessAdjust));
						path.LineTo((float)(area.X + area.Width - borderThickness.Right), (float)(area.Y + StrokeThicknessAdjust));
						path.Close();
					});
				}

				if (borderThickness.Bottom != 0)
				{
					createLayer((l, path) =>
					{
						l.StrokeThickness = (float)borderThickness.Bottom;
						var StrokeThicknessAdjust = borderThickness.Bottom / 2;
						path.MoveTo((float)(area.X + (float)borderThickness.Left), (float)(area.Y + area.Height - StrokeThicknessAdjust));
						path.LineTo((float)(area.X + area.Width - (float)borderThickness.Right), (float)(area.Y + area.Height - StrokeThicknessAdjust));
						path.Close();
					});
				}

				if (borderThickness.Left != 0)
				{
					createLayer((l, path) =>
					{
						l.StrokeThickness = (float)borderThickness.Left;
						var StrokeThicknessAdjust = borderThickness.Left / 2;
						path.MoveTo((float)(area.X + StrokeThicknessAdjust), (float)area.Y);
						path.LineTo((float)(area.X + StrokeThicknessAdjust), (float)(area.Y + area.Height));
						path.Close();
					});
				}

				if (borderThickness.Right != 0)
				{
					createLayer((l, path) =>
					{
						l.StrokeThickness = (float)borderThickness.Right;
						var StrokeThicknessAdjust = borderThickness.Right / 2;
						path.MoveTo((float)(area.X + area.Width - StrokeThicknessAdjust), (float)area.Y);
						path.LineTo((float)(area.X + area.Width - StrokeThicknessAdjust), (float)(area.Y + area.Height));
						path.Close();
					});
				}
			}

			sublayers.Add(shapeVisual);

			// Must be inserted below the other subviews, which may happen when
			// the current view has subviews.
			parent.Children.InsertAtBottom(shapeVisual);
		}

		disposables.Add(() =>
		{
			owner.ClippingIsSetByCornerRadius = false;

			foreach (var sv in sublayers)
			{
				parent.Children.Remove(sv);
				sv.Dispose();
			}
		}
		);

		compositor.InvalidateRender();

		return disposables;
	}

	private static Rect CreateImageLayer(Compositor compositor, CompositeDisposable disposables, Thickness borderThickness, Rect adjustedArea, CompositionSpriteShape backgroundShape, Rect backgroundArea, ImageBrush imgBackground)
	{
		imgBackground.Subscribe(imageData =>
		{

			if (imageData.Error is null)
			{
				var surfaceBrush = compositor.CreateSurfaceBrush(imageData.CompositionSurface);

				var sourceImageSize = new Size(imageData.CompositionSurface.Image.Width, imageData.CompositionSurface.Image.Height);

				// We reduce the adjustedArea again so that the image is inside the border (like in Windows)
				var imageArea = adjustedArea.DeflateBy(borderThickness);

				backgroundArea = imgBackground.GetArrangedImageRect(sourceImageSize, imageArea);

				// surfaceBrush.Offset = new Vector2((float)imageFrame.Left, (float)imageFrame.Top);
				var matrix = Matrix3x2.CreateScale((float)(backgroundArea.Width / sourceImageSize.Width), (float)(backgroundArea.Height / sourceImageSize.Height));
				matrix *= Matrix3x2.CreateTranslation((float)backgroundArea.Left, (float)backgroundArea.Top);

				if (imgBackground.Transform != null)
				{
					matrix *= imgBackground.Transform.ToMatrix(new Point());
				}

				surfaceBrush.TransformMatrix = matrix;

				backgroundShape.FillBrush = surfaceBrush;
			}
			else
			{
				backgroundShape.FillBrush = null;
			}
		}).DisposeWith(disposables);
		return backgroundArea;
	}


	private static CompositionPath GetRoundedPath(SKRect area, SKPoint[] radii, SkiaGeometrySource2D geometrySource = null)
	{
		geometrySource ??= new SkiaGeometrySource2D();
		var geometry = geometrySource.Geometry;

		var roundRect = CreateRoundRect(area, radii);
		geometry.AddRoundRect(roundRect);
		geometry.Close();

		return new CompositionPath(geometrySource);
	}

	private static SKRoundRect CreateRoundRect(SKRect area, SKPoint[] radii)
	{
		var roundRect = new SKRoundRect();
		roundRect.SetRectRadii(area, radii);
		return roundRect;
	}

	private static CompositionPath GetBorderPath(SKPoint[] outerRadii, SKPoint[] innerRadii, Rect area, Rect insetArea)
	{
		var geometrySource = new SkiaGeometrySource2D();
		GetRoundedPath(area.ToSKRect(), outerRadii, geometrySource);
		GetRoundedPath(insetArea.ToSKRect(), innerRadii, geometrySource);
		geometrySource.Geometry.FillType = SKPathFillType.EvenOdd;

		return new CompositionPath(geometrySource);
	}
}
