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

namespace Windows.UI.Xaml.Shapes
{
	partial class BorderLayerRenderer
	{
		private LayoutState _currentState;

		private SerialDisposable _layerDisposable = new SerialDisposable();

		/// <summary>
		/// Updates or creates a sublayer to render a border-like shape.
		/// </summary>
		/// <param name="owner">The parent layer to apply the shape</param>
		/// <param name="background">The background brush</param>
		/// <param name="borderThickness">The border thickness</param>
		/// <param name="borderBrush">The border brush</param>
		/// <param name="cornerRadius">The corner radius</param>
		/// <param name="backgroundImage">The background image in case of a ImageBrush background</param>
		public void UpdateLayer(
			FrameworkElement owner,
			Brush background,
			BackgroundSizing backgroundSizing,
			Thickness borderThickness,
			Brush borderBrush,
			CornerRadius cornerRadius,
			object backgroundImage
		)
		{
			// Bounds is captured to avoid calling twice calls below.
			var area = new Rect(0, 0, owner.ActualWidth, owner.ActualHeight);

			var newState = new LayoutState(area, background, backgroundSizing, borderThickness, borderBrush, cornerRadius, backgroundImage);
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

				var outerRadii = GetOuterRadii(cornerRadius);
				var innerRadii = GetInnerRadii(cornerRadius, borderThickness);

				var borderPath = GetRoundedRect(outerRadii, innerRadii, borderThickness, area, adjustedArea);
				
				var backgroundPath = state.BackgroundSizing == BackgroundSizing.InnerBorderEdge ?
					GetRoundedPath(adjustedArea.ToSKRect(), innerRadii) :
					GetRoundedPath(adjustedArea.ToSKRect(), outerRadii);

				var outerPath = GetRoundedPath(area.ToSKRect(), outerRadii);

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

		/// <summary>
		/// Creates a rounded-rectangle path from the nominated bounds and corner radius.
		/// </summary>
		private static CompositionPath GetRoundedPath(SKRect area, SKPoint[] radii, SkiaGeometrySource2D geometrySource = null)
		{
			geometrySource ??= new SkiaGeometrySource2D();
			var geometry = geometrySource.Geometry;

			// How ArcTo works:
			// http://www.twistedape.me.uk/blog/2013/09/23/what-arctopointdoes/

			var roundRect = new SKRoundRect();
			roundRect.SetRectRadii(area, radii);
			geometry.AddRoundRect(roundRect);
			geometry.Close();

			return new CompositionPath(geometrySource);
		}

		internal static SKPoint[] GetOuterRadii(CornerRadius cornerRadius) => new SKPoint[]
			{
				new SKPoint((float)cornerRadius.TopLeft, (float)cornerRadius.TopLeft),
				new SKPoint((float)cornerRadius.TopRight, (float)cornerRadius.TopRight),
				new SKPoint((float)cornerRadius.BottomRight, (float)cornerRadius.BottomRight),
				new SKPoint((float)cornerRadius.BottomLeft, (float)cornerRadius.BottomLeft)
			};

		internal static SKPoint[] GetInnerRadii(CornerRadius cornerRadius, Thickness borderThickness)
		{
			// For most cases :
			// ICR: Inner CornerRadius
			// OCR: Outer CornerRadius
			// BT: BorderThickness
			// ICR = OCR - BT

			// TODO : Manage the case where BorderThickness >= CornerRadius
			// See https://github.com/unoplatform/uno/issues/6891 for more details

			return new SKPoint[]
			{
				new SKPoint((float)Math.Max(0, cornerRadius.TopLeft - borderThickness.Top / 2), (float)Math.Max(cornerRadius.TopLeft - borderThickness.Left / 2)),
				new SKPoint((float)Math.Max(cornerRadius.TopRight - borderThickness.Top / 2), (float)Math.Max(cornerRadius.TopRight - borderThickness.Right / 2)),
				new SKPoint((float)Math.Max(cornerRadius.BottomRight - borderThickness.Bottom / 2), (float)Math.Max(cornerRadius.BottomRight - borderThickness.Right / 2)),
				new SKPoint((float)Math.Max(cornerRadius.BottomLeft - borderThickness.Bottom / 2), (float)Math.Max(cornerRadius.BottomLeft - borderThickness.Left / 2))
			};
		}

		private static CompositionPath GetRoundedRect(SKPoint[] outerRadii, SKPoint[] innerRadii, Thickness borderThickness, Rect area, Rect insetArea)
		{
			var geometrySource = new SkiaGeometrySource2D();

			GetRoundedPath(area.ToSKRect(), outerRadii, geometrySource);
			GetRoundedPath(insetArea.ToSKRect(), innerRadii, geometrySource);
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
}
