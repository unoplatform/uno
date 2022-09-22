using System;
using System.Collections.Generic;
using Uno.Disposables;
using System.Text;
using Uno;
using Uno.Extensions;
using Windows.UI.Xaml.Media;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Composition;
using System.Numerics;
using Uno.UI;
using SkiaSharp;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

internal partial class BorderLayerRenderer
{
	private readonly SerialDisposable _layerDisposable = new SerialDisposable();

	private LayoutState _currentState;

	/// <summary>
	/// Updates or creates a sublayer to render a border-like shape.
	/// </summary>
	partial void UpdateLayer()
	{
		if (!_owner.IsLoaded)
		{
			// Avoid creating layer until actually usable.
			return;
		}

		// Bounds is captured to avoid calling twice calls below.
		var size = _owner.ActualSize;
		var area = new Rect(0, 0, size.X, size.Y);

		var newState = new LayoutState(
			area,
			_borderInfoProvider.Background,
			_borderInfoProvider.BackgroundSizing,
			_borderInfoProvider.BorderThickness,
			_borderInfoProvider.BorderBrush,
			_borderInfoProvider.CornerRadius,
			_borderInfoProvider.BackgroundImage);
		
		var previousLayoutState = _currentState;

		if (!newState.Equals(previousLayoutState))
		{
			if (
				_borderInfoProvider.Background != null ||
				_borderInfoProvider.CornerRadius != CornerRadius.None ||
				(_borderInfoProvider.BorderThickness != Thickness.Empty && _borderInfoProvider.BorderBrush != null)
			)
			{

				_layerDisposable.Disposable = null;
				_layerDisposable.Disposable = InnerCreateLayer(_owner, newState);
			}
			else
			{
				_layerDisposable.Disposable = null;
			}

			_currentState = newState;
		}
	}

	/// <summary>
	/// Removes the added layers during a call to <see cref="UpdateLayer" />.
	/// </summary>
	partial void ClearLayer()
	{
		_layerDisposable.Disposable = null;
		_currentState = null;
	}

	private static IDisposable InnerCreateLayer(UIElement owner, LayoutState state)
	{
		var parent = owner.Visual;
		var compositor = parent.Compositor;
		var area = owner.LayoutRound(state.Area);
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

		if (cornerRadius != CornerRadius.None)
		{
			var maxOuterRadius = Math.Max(0, Math.Min(halfWidth - widthOffset, halfHeight - heightOffset));
			var maxInnerRadius = Math.Max(0, Math.Min(halfWidth, halfHeight));

			cornerRadius = new CornerRadius(
				Math.Min(cornerRadius.TopLeft, maxOuterRadius),
				Math.Min(cornerRadius.TopRight, maxOuterRadius),
				Math.Min(cornerRadius.BottomRight, maxOuterRadius),
				Math.Min(cornerRadius.BottomLeft, maxOuterRadius));

			var innerCornerRadius = new CornerRadius(
				Math.Min(cornerRadius.TopLeft, maxInnerRadius),
				Math.Min(cornerRadius.TopRight, maxInnerRadius),
				Math.Min(cornerRadius.BottomRight, maxInnerRadius),
				Math.Min(cornerRadius.BottomLeft, maxInnerRadius));

			var borderShape = compositor.CreateSpriteShape();
			var backgroundShape = compositor.CreateSpriteShape();
			var outerShape = compositor.CreateSpriteShape();

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

			var borderPath = GetRoundedRect(cornerRadius, innerCornerRadius, area, adjustedArea);
			var backgroundPath = GetRoundedPath(cornerRadius, adjustedArea);
			var outerPath = GetRoundedPath(cornerRadius, area);

			backgroundShape.Geometry = compositor.CreatePathGeometry(backgroundPath);
			borderShape.Geometry = compositor.CreatePathGeometry(borderPath);
			outerShape.Geometry = compositor.CreatePathGeometry(outerPath);

			var borderVisual = compositor.CreateShapeVisual();
			var backgroundVisual = compositor.CreateShapeVisual();
			backgroundVisual.Shapes.Add(backgroundShape);
			borderVisual.Shapes.Add(borderShape);

			sublayers.Add(backgroundVisual);
			sublayers.Add(borderVisual);
			parent.Children.InsertAtBottom(backgroundVisual);
			parent.Children.InsertAtTop(borderVisual);

			owner.ClippingIsSetByCornerRadius = cornerRadius != CornerRadius.None;
			if (owner.ClippingIsSetByCornerRadius)
			{
				parent.Clip = compositor.CreateGeometricClip(outerShape.Geometry);
			}
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
				var surfaceBrush = compositor.CreateSurfaceBrush(imageData.Value);

				var sourceImageSize = new Size(imageData.Value.Image.Width, imageData.Value.Image.Height);

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

	/// <summary>
	/// Creates a rounded-rectangle path from the nominated bounds and corner radius.
	/// </summary>
	private static CompositionPath GetRoundedPath(CornerRadius cornerRadius, Rect area, SkiaGeometrySource2D geometrySource = null)
	{
		geometrySource ??= new SkiaGeometrySource2D();
		var geometry = geometrySource.Geometry;

		// How ArcTo works:
		// http://www.twistedape.me.uk/blog/2013/09/23/what-arctopointdoes/

		geometry.MoveTo((float)area.GetMidX(), (float)area.Y);
		geometry.ArcTo((float)area.Right, (float)area.Top, (float)area.Right, (float)area.GetMidY(), (float)cornerRadius.TopRight);
		geometry.ArcTo((float)area.Right, (float)area.Bottom, (float)area.GetMidX(), (float)area.Bottom, (float)cornerRadius.BottomRight);
		geometry.ArcTo((float)area.Left, (float)area.Bottom, (float)area.Left, (float)area.GetMidY(), (float)cornerRadius.BottomLeft);
		geometry.ArcTo((float)area.Left, (float)area.Top, (float)area.GetMidX(), (float)area.Top, (float)cornerRadius.TopLeft);

		geometry.Close();

		return new CompositionPath(geometrySource);
	}

	private static CompositionPath GetRoundedRect(CornerRadius cornerRadius, CornerRadius innerCornerRadius, Rect area, Rect insetArea)
	{
		var geometrySource = new SkiaGeometrySource2D();

		GetRoundedPath(cornerRadius, area, geometrySource);
		GetRoundedPath(innerCornerRadius, insetArea, geometrySource);
		geometrySource.Geometry.FillType = SKPathFillType.EvenOdd;
		return new CompositionPath(geometrySource);
	}

	private class LayoutState : IEquatable<LayoutState>
	{
		public readonly Rect Area;
		public readonly Brush Background;
		public readonly BackgroundSizing BackgroundSizing;
		public readonly Brush BorderBrush;
		public readonly Thickness BorderThickness;
		public readonly CornerRadius CornerRadius;
		public readonly object BackgroundImage;

		public LayoutState(Rect area, Brush background, BackgroundSizing backgroundSizing,
			Thickness borderThickness, Brush borderBrush, CornerRadius cornerRadius, object backgroundImage)
		{
			Area = area;
			Background = background;
			BackgroundSizing = backgroundSizing;
			BorderBrush = borderBrush;
			CornerRadius = cornerRadius;
			BorderThickness = borderThickness;
			BackgroundImage = backgroundImage;
		}

		public bool Equals(LayoutState other)
		{
			return other != null
				&& other.Area == Area
				&& other.Background == Background
				&& other.BackgroundSizing == BackgroundSizing
				&& other.BorderBrush == BorderBrush
				&& other.BorderThickness == BorderThickness
				&& other.CornerRadius == CornerRadius
				&& other.BackgroundImage == BackgroundImage;
		}
	}
}
