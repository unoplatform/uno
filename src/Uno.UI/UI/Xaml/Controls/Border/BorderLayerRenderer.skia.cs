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

namespace Windows.UI.Xaml.Shapes
{
	internal class BorderLayerRenderer
	{
		private LayoutState _currentState;

		private SerialDisposable _layerDisposable = new SerialDisposable();

		/// <summary>
		/// Updates or creates a sublayer to render a border-like shape.
		/// </summary>
		/// <param name="owner">The parent layer to apply the shape</param>
		/// <param name="area">The rendering area</param>
		/// <param name="background">The background brush</param>
		/// <param name="borderThickness">The border thickness</param>
		/// <param name="borderBrush">The border brush</param>
		/// <param name="cornerRadius">The corner radius</param>
		/// <param name="backgroundImage">The background image in case of a ImageBrush background</param>
		public void UpdateLayer(
			FrameworkElement owner,
			Brush background,
			Thickness borderThickness,
			Brush borderBrush,
			CornerRadius cornerRadius,
			object backgroundImage
		)
		{
			// Bounds is captured to avoid calling twice calls below.
			var area = new Rect(0, 0, owner.ActualWidth, owner.ActualHeight);

			var newState = new LayoutState(area, background, borderThickness, borderBrush, cornerRadius, backgroundImage);
			var previousLayoutState = _currentState;

			if (!newState.Equals(previousLayoutState))
			{
				if (
					background != null ||
					cornerRadius != CornerRadius.None ||
					(borderThickness != Thickness.Empty && borderBrush != null)
				)
				{

					_layerDisposable.Disposable = null;
					_layerDisposable.Disposable = InnerCreateLayer(owner, newState);
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
		internal void Clear()
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
			var adjustedArea = area;
			adjustedArea = adjustedArea.DeflateBy(borderThickness);

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

				Brush.AssignAndObserveBrush(borderBrush, color => borderShape.FillBrush = compositor.CreateColorBrush(color))
					.DisposeWith(disposables);

				if (background is GradientBrush gradientBackground)
				{
					//var fillMask = new CAShapeLayer()
					//{
					//	Path = path,
					//	Frame = area,
					//	// We only use the fill color to create the mask area
					//	FillColor = _Color.White.CGColor,
					//};
					//// We reduce the adjustedArea again so that the gradient is inside the border (like in Windows)
					//adjustedArea = adjustedArea.Shrink((float)adjustedStrokeThicknessOffset);

					//CreateGradientBrushLayers(area, adjustedArea, parent, sublayers, ref insertionIndex, gradientBackground, fillMask);
				}
				else if (background is SolidColorBrush scbBackground)
				{
					Brush.AssignAndObserveBrush(scbBackground, color => backgroundShape.FillBrush = compositor.CreateColorBrush(color))
						.DisposeWith(disposables);
				}
				else if (background is ImageBrush imgBackground)
				{
					adjustedArea = CreateImageLayer(compositor, disposables, borderThickness, adjustedArea, backgroundShape, adjustedArea, imgBackground);
				}
				else if (background is AcrylicBrush acrylicBrush)
				{
					Brush.AssignAndObserveBrush(acrylicBrush, color => backgroundShape.FillBrush = compositor.CreateColorBrush(color))
						.DisposeWith(disposables);
				}
				else if (background is XamlCompositionBrushBase unsupportedCompositionBrush)
				{
					Brush.AssignAndObserveBrush(unsupportedCompositionBrush, color => backgroundShape.FillBrush = compositor.CreateColorBrush(color))
						.DisposeWith(disposables);
				}
				else
				{
					backgroundShape.FillBrush = null;
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

				if (background is GradientBrush gradientBackground)
				{
					var fullArea = new Rect(
						area.X + borderThickness.Left,
						area.Y + borderThickness.Top,
						area.Width - borderThickness.Left - borderThickness.Right,
						area.Height - borderThickness.Top - borderThickness.Bottom);

					var insideArea = new Rect(default, fullArea.Size);
					// var insertionIndex = 0;

					// CreateGradientBrushLayers(fullArea, insideArea, parent, sublayers, ref insertionIndex, gradientBackground, fillMask: null);
				}
				else if (background is SolidColorBrush scbBackground)
				{
					Brush.AssignAndObserveBrush(scbBackground, c => backgroundShape.FillBrush = compositor.CreateColorBrush(c))
						.DisposeWith(disposables);

					// This is required because changing the CornerRadius changes the background drawing 
					// implementation and we don't want a rectangular background behind a rounded background.
					Disposable.Create(() => backgroundShape.FillBrush = null)
						.DisposeWith(disposables);
				}
				else if (background is ImageBrush imgBackground)
				{
					backgroundArea = CreateImageLayer(compositor, disposables, borderThickness, adjustedArea, backgroundShape, backgroundArea, imgBackground);
				}
				else if (background is AcrylicBrush acrylicBrush)
				{
					Brush.AssignAndObserveBrush(acrylicBrush, c => backgroundShape.FillBrush = compositor.CreateColorBrush(c))
						.DisposeWith(disposables);

					Disposable.Create(() => backgroundShape.FillBrush = null)
						.DisposeWith(disposables);
				}
				else if (background is XamlCompositionBrushBase unsupportedCompositionBrush)
				{
					Brush.AssignAndObserveBrush(unsupportedCompositionBrush, c => backgroundShape.FillBrush = compositor.CreateColorBrush(c))
						.DisposeWith(disposables);

					// This is required because changing the CornerRadius changes the background drawing 
					// implementation and we don't want a rectangular background behind a rounded background.
					Disposable.Create(() => backgroundShape.FillBrush = null)
						.DisposeWith(disposables);
				}
				else
				{
					backgroundShape.FillBrush = null;
				}

				var geometrySource = new SkiaGeometrySource2D();
				var geometry = geometrySource.Geometry;

				geometry.AddRect(backgroundArea.ToSKRect());

				backgroundShape.Geometry = compositor.CreatePathGeometry(new CompositionPath(geometrySource));

				shapeVisual.Shapes.Add(backgroundShape);

				if (borderThickness != Thickness.Empty)
				{
					Action<Action<CompositionSpriteShape, SkiaSharp.SKPath>> createLayer = builder =>
					{
						var spriteShape = compositor.CreateSpriteShape();
						var geometry = new SkiaGeometrySource2D();

						Brush.AssignAndObserveBrush(borderBrush, c => spriteShape.StrokeBrush = compositor.CreateColorBrush(c))
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
			public readonly Brush BorderBrush;
			public readonly Thickness BorderThickness;
			public readonly CornerRadius CornerRadius;
			public readonly object BackgroundImage;

			public LayoutState(Rect area, Brush background, Thickness borderThickness, Brush borderBrush, CornerRadius cornerRadius, object backgroundImage)
			{
				Area = area;
				Background = background;
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
					&& other.BorderBrush == BorderBrush
					&& other.BorderThickness == BorderThickness
					&& other.CornerRadius == CornerRadius
					&& other.BackgroundImage == BackgroundImage;
			}
		}
	}
}
