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
					(borderThickness != Thickness.Empty && borderBrush != null)
				)
				{

					_layerDisposable.Disposable = null;
					_layerDisposable.Disposable = InnerCreateLayer(owner.Visual, area, background, borderThickness, borderBrush, cornerRadius);
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

		private static IDisposable InnerCreateLayer(
			ContainerVisual parent,
			Rect area,
			Brush background,
			Thickness borderThickness,
			Brush borderBrush,
			CornerRadius cornerRadius)
		{
			var disposables = new CompositeDisposable();
			var subVisuals = new List<Visual>();

			var adjustedLineWidth = borderThickness.Top;
			var adjustedLineWidthOffset = adjustedLineWidth / 2;

			var adjustedArea = area;
			adjustedArea.Inflate(-adjustedLineWidthOffset, -adjustedLineWidthOffset);

			var compositor = parent.Compositor;

			var shapeVisual = compositor.CreateShapeVisual();
			parent.Children.InsertAtBottom(shapeVisual);

			var spriteShape = compositor.CreateSpriteShape();

			SkiaGeometrySource2D BuildGeometry()
			{
				var maxRadius = Math.Max(0, Math.Min((float)area.Width / 2 - adjustedLineWidthOffset, (float)area.Height / 2 - adjustedLineWidthOffset));
				cornerRadius = new CornerRadius(
					Math.Min(cornerRadius.TopLeft, maxRadius),
					Math.Min(cornerRadius.TopRight, maxRadius),
					Math.Min(cornerRadius.BottomRight, maxRadius),
					Math.Min(cornerRadius.BottomLeft, maxRadius));

				var geometry = new SkiaGeometrySource2D();

				Brush.AssignAndObserveBrush(borderBrush, color => spriteShape.StrokeBrush = compositor.CreateColorBrush(color))
					.DisposeWith(disposables);

				geometry.Geometry.MoveTo((float)adjustedArea.GetMidX(), (float)adjustedArea.Y);
				geometry.Geometry.ArcTo((float)adjustedArea.Right, (float)adjustedArea.Top, (float)adjustedArea.Right, (float)adjustedArea.GetMidY(), (float)cornerRadius.TopRight);
				geometry.Geometry.ArcTo((float)adjustedArea.Right, (float)adjustedArea.Bottom, (float)adjustedArea.GetMidX(), (float)adjustedArea.Bottom, (float)cornerRadius.BottomRight);
				geometry.Geometry.ArcTo((float)adjustedArea.Left, (float)adjustedArea.Bottom, (float)adjustedArea.Left, (float)adjustedArea.GetMidY(), (float)cornerRadius.BottomLeft);
				geometry.Geometry.ArcTo((float)adjustedArea.Left, (float)adjustedArea.Top, (float)adjustedArea.GetMidX(), (float)adjustedArea.Top, (float)cornerRadius.TopLeft);

				geometry.Geometry.Close();

				if (background is LinearGradientBrush lgbBackground)
				{
					//var fillMask = new CAShapeLayer()
					//{
					//	Path = path,
					//	Frame = area,
					//	// We only use the fill color to create the mask area
					//	FillColor = _Color.White.CGColor,
					//};

					//// We reduce the adjustedArea again so that the gradient is inside the border (like in Windows)
					//adjustedArea = adjustedArea.Shrink((nfloat)adjustedLineWidthOffset);

					//CreateLinearGradientBrushLayers(area, adjustedArea, parent, sublayers, ref insertionIndex, lgbBackground, fillMask);
				}
				else if (background is SolidColorBrush scbBackground)
				{
					Brush.AssignAndObserveBrush(scbBackground, color => spriteShape.FillBrush = compositor.CreateColorBrush(color))
						.DisposeWith(disposables);
				}
				else if (background is ImageBrush imgBackground)
				{
					//var uiImage = imgBackground.ImageSource?.ImageData;
					//if (uiImage != null && uiImage.Size != CGSize.Empty)
					//{
					//	var fillMask = new CAShapeLayer()
					//	{
					//		Path = path,
					//		Frame = area,
					//		// We only use the fill color to create the mask area
					//		FillColor = _Color.White.CGColor,
					//	};

					//	// We reduce the adjustedArea again so that the image is inside the border (like in Windows)
					//	adjustedArea = adjustedArea.Shrink((nfloat)adjustedLineWidthOffset);

					//	CreateImageBrushLayers(area, adjustedArea, parent, sublayers, ref insertionIndex, imgBackground, fillMask);
					//}
				}
				else
				{
					spriteShape.FillBrush = null;
				}

				return geometry;
			}

			spriteShape.Geometry = compositor.CreatePathGeometry(new CompositionPath(BuildGeometry()));

			shapeVisual.Size = new Vector2((float)area.Width, (float)area.Height);
			shapeVisual.Offset = new Vector3(0, 0, 0);

			shapeVisual.Shapes.Add(spriteShape);

			subVisuals.Add(shapeVisual);

			disposables.Add(() =>
			{
				foreach (var sv in subVisuals)
				{
					parent.Children.Remove(sv);
					sv.Dispose();
				}
			});

			Window.Current.QueueInvalidateRender();

			return disposables;
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
