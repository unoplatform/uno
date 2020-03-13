using CoreAnimation;
using CoreGraphics;
using System;
using System.Collections.Generic;
using Uno.Disposables;
using Windows.UI.Xaml.Media;
using Uno.UI.Extensions;

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
using _Color = UIKit.UIColor;
using _Image = UIKit.UIImage;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
using _Color = AppKit.NSColor;
using _Image = AppKit.NSImage;
#endif

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
		public CGPath UpdateLayer(
			_View owner,
			Brush background,
			Thickness borderThickness,
			Brush borderBrush,
			CornerRadius cornerRadius,
			_Image backgroundImage
		)
		{
			// Bounds is captured to avoid calling twice calls below.
			var bounds = owner.Bounds;
			var area = new CGRect(0, 0, bounds.Width, bounds.Height);

			var newState = new LayoutState(area, background, borderThickness, borderBrush, cornerRadius, backgroundImage);
			var previousLayoutState = _currentState;

			if (!newState.Equals(previousLayoutState))
			{
#if __MACOS__
				owner.WantsLayer = true;
#endif

				_layerDisposable.Disposable = null;
				_layerDisposable.Disposable = InnerCreateLayer(owner as UIElement, owner.Layer, newState);

				_currentState = newState;
			}

			return newState.BoundsPath;
		}

		/// <summary>
		/// Removes the added layers during a call to <see cref="UpdateLayer" />.
		/// </summary>
		internal void Clear()
		{
			_layerDisposable.Disposable = null;
			_currentState = null;
		}

		private static IDisposable InnerCreateLayer(UIElement owner, CALayer parent, LayoutState state)
		{

			var area = state.Area;
			var background = state.Background;
			var borderThickness = state.BorderThickness;
			var borderBrush = state.BorderBrush;
			var cornerRadius = state.CornerRadius;

			var disposables = new CompositeDisposable();
			var sublayers = new List<CALayer>();

			var adjustedLineWidth = borderThickness.Top;
			var adjustedLineWidthOffset = adjustedLineWidth / 2;

			var adjustedArea = area;
			adjustedArea = adjustedArea.Shrink((nfloat)adjustedLineWidthOffset);

			if (cornerRadius != CornerRadius.None)
			{
				var maxRadius = Math.Max(0, Math.Min((float)area.Width / 2 - adjustedLineWidthOffset, (float)area.Height / 2 - adjustedLineWidthOffset));
				cornerRadius = new CornerRadius(
					Math.Min(cornerRadius.TopLeft, maxRadius),
					Math.Min(cornerRadius.TopRight, maxRadius),
					Math.Min(cornerRadius.BottomRight, maxRadius),
					Math.Min(cornerRadius.BottomLeft, maxRadius));

				CAShapeLayer layer = new CAShapeLayer();
				layer.LineWidth = (nfloat)adjustedLineWidth;
				layer.FillColor = null;


				Brush.AssignAndObserveBrush(borderBrush, color => layer.StrokeColor = color)
					.DisposeWith(disposables);
				var path = GetRoundedPath(cornerRadius, adjustedArea);

				var outerPath = GetRoundedPath(cornerRadius, area);

				var lgbBackground = background as LinearGradientBrush;
				var scbBackground = background as SolidColorBrush;
				var imgBackground = background as ImageBrush;
				var insertionIndex = 0;

				if (lgbBackground != null)
				{
					var fillMask = new CAShapeLayer()
					{
						Path = path,
						Frame = area,
						// We only use the fill color to create the mask area
						FillColor = _Color.White.CGColor,
					};
					// We reduce the adjustedArea again so that the gradient is inside the border (like in Windows)
					adjustedArea = adjustedArea.Shrink((nfloat)adjustedLineWidthOffset);

					CreateLinearGradientBrushLayers(area, adjustedArea, parent, sublayers, ref insertionIndex, lgbBackground, fillMask);
				}
				else if (scbBackground != null)
				{
					Brush.AssignAndObserveBrush(scbBackground, color => layer.FillColor = color)
						.DisposeWith(disposables);
				}
				else if (imgBackground != null)
				{
					var uiImage = imgBackground.ImageSource?.ImageData;
					if (uiImage != null && uiImage.Size != CGSize.Empty)
					{
						var fillMask = new CAShapeLayer()
						{
							Path = path,
							Frame = area,
							// We only use the fill color to create the mask area
							FillColor = _Color.White.CGColor,
						};
						// We reduce the adjustedArea again so that the image is inside the border (like in Windows)
						adjustedArea = adjustedArea.Shrink((nfloat)adjustedLineWidthOffset);

						CreateImageBrushLayers(area, adjustedArea, parent, sublayers, ref insertionIndex, imgBackground, fillMask);
					}
				}
				else
				{
					layer.FillColor = Colors.Transparent;
				}

				layer.Path = path;

				sublayers.Add(layer);
				parent.InsertSublayer(layer, insertionIndex);

				parent.Mask = new CAShapeLayer()
				{
					Path = outerPath,
					Frame = area,
					// We only use the fill color to create the mask area
					FillColor = _Color.White.CGColor,
				};

				if (owner != null)
				{
					owner.ClippingIsSetByCornerRadius = true;
				}

				state.BoundsPath = path;
			}
			else
			{
				var lgbBackground = background as LinearGradientBrush;
				var scbBackground = background as SolidColorBrush;
				var imgBackground = background as ImageBrush;

				if (lgbBackground != null)
				{
					var fullArea = new CGRect(
						area.X + borderThickness.Left,
						area.Y + borderThickness.Top,
						area.Width - borderThickness.Left - borderThickness.Right,
						area.Height - borderThickness.Top - borderThickness.Bottom);

					var insideArea = new CGRect(CGPoint.Empty, fullArea.Size);
					var insertionIndex = 0;

					CreateLinearGradientBrushLayers(fullArea, insideArea, parent, sublayers, ref insertionIndex, lgbBackground, fillMask: null);
				}
				else if (scbBackground != null)
				{
					Brush.AssignAndObserveBrush(scbBackground, c => parent.BackgroundColor = c)
						.DisposeWith(disposables);

					// This is required because changing the CornerRadius changes the background drawing 
					// implementation and we don't want a rectangular background behind a rounded background.
					Disposable.Create(() => parent.BackgroundColor = null)
						.DisposeWith(disposables);
				}
				else if (imgBackground != null)
				{
					var uiImage = imgBackground.ImageSource?.ImageData;
					if (uiImage != null && uiImage.Size != CGSize.Empty)
					{
						var fullArea = new CGRect(
								area.X + borderThickness.Left,
								area.Y + borderThickness.Top,
								area.Width - borderThickness.Left - borderThickness.Right,
								area.Height - borderThickness.Top - borderThickness.Bottom);

						var insideArea = new CGRect(CGPoint.Empty, fullArea.Size);
						var insertionIndex = 0;

						CreateImageBrushLayers(fullArea, insideArea, parent, sublayers, ref insertionIndex, imgBackground, fillMask: null);
					}
				}
				else
				{
					parent.BackgroundColor = Colors.Transparent;
				}

				if (borderThickness != Thickness.Empty)
				{
					Action<Action<CAShapeLayer, CGPath>> createLayer = builder =>
					{
						CAShapeLayer layer = new CAShapeLayer();
						var path = new CGPath();

						Brush.AssignAndObserveBrush(borderBrush, c => layer.StrokeColor = c)
							.DisposeWith(disposables);

						builder(layer, path);
						layer.Path = path;

						// Must be inserted below the other subviews, which may happen when
						// the current view has subviews.
						sublayers.Add(layer);
						parent.InsertSublayer(layer, 0);
					};

					if (borderThickness.Top != 0)
					{
						createLayer((l, path) =>
						{
							l.LineWidth = (nfloat)borderThickness.Top;
							var lineWidthAdjust = (nfloat)(borderThickness.Top / 2);
							path.MoveToPoint(area.X + (nfloat)borderThickness.Left, area.Y + lineWidthAdjust);
							path.AddLineToPoint(area.X + area.Width - (nfloat)borderThickness.Right, area.Y + lineWidthAdjust);
							path.CloseSubpath();
						});
					}

					if (borderThickness.Bottom != 0)
					{
						createLayer((l, path) =>
						{
							l.LineWidth = (nfloat)borderThickness.Bottom;
							var lineWidthAdjust = borderThickness.Bottom / 2;
							path.MoveToPoint(area.X + (nfloat)borderThickness.Left, (nfloat)(area.Y + area.Height - lineWidthAdjust));
							path.AddLineToPoint(area.X + area.Width - (nfloat)borderThickness.Right, (nfloat)(area.Y + area.Height - lineWidthAdjust));
							path.CloseSubpath();
						});
					}

					if (borderThickness.Left != 0)
					{
						createLayer((l, path) =>
						{
							l.LineWidth = (nfloat)borderThickness.Left;
							var lineWidthAdjust = borderThickness.Left / 2;
							path.MoveToPoint((nfloat)(area.X + lineWidthAdjust), area.Y);
							path.AddLineToPoint((nfloat)(area.X + lineWidthAdjust), area.Y + area.Height);
							path.CloseSubpath();
						});
					}

					if (borderThickness.Right != 0)
					{
						createLayer((l, path) =>
						{
							l.LineWidth = (nfloat)borderThickness.Right;
							var lineWidthAdjust = borderThickness.Right / 2;
							path.MoveToPoint((nfloat)(area.X + area.Width - lineWidthAdjust), area.Y);
							path.AddLineToPoint((nfloat)(area.X + area.Width - lineWidthAdjust), area.Y + area.Height);
							path.CloseSubpath();
						});
					}
				}

				state.BoundsPath = CGPath.FromRect(parent.Bounds);
			}

			disposables.Add(() =>
			{
				foreach (var sl in sublayers)
				{
					sl.RemoveFromSuperLayer();
					sl.Dispose();
				}

				if (owner != null)
				{
					owner.ClippingIsSetByCornerRadius = false;
				}
			}
			);
			return disposables;
		}

		/// <summary>
		/// Creates a rounded-rectangle path from the nominated bounds and corner radius.
		/// </summary>
		private static CGPath GetRoundedPath(CornerRadius cornerRadius, CGRect area)
		{
			var path = new CGPath();
			// How AddArcToPoint works:
			// http://www.twistedape.me.uk/blog/2013/09/23/what-arctopointdoes/

			path.MoveToPoint(area.GetMidX(), area.Y);
			path.AddArcToPoint(area.Right, area.Top, area.Right, area.GetMidY(), (float)cornerRadius.TopRight);
			path.AddArcToPoint(area.Right, area.Bottom, area.GetMidX(), area.Bottom, (float)cornerRadius.BottomRight);
			path.AddArcToPoint(area.Left, area.Bottom, area.Left, area.GetMidY(), (float)cornerRadius.BottomLeft);
			path.AddArcToPoint(area.Left, area.Top, area.GetMidX(), area.Top, (float)cornerRadius.TopLeft);

			path.CloseSubpath();
			return path;
		}

		/// <summary>
		/// Creates and add 2 layers for ImageBrush background
		/// </summary>
		/// <param name="fullArea">The full area available (includes the border)</param>
		/// <param name="insideArea">The "inside" of the border (excludes the border)</param>
		/// <param name="layer">The layer in which the image layers will be added</param>
		/// <param name="sublayers">List of layers to keep all references</param>
		/// <param name="insertionIndex">Where in the layer the new layers will be added</param>
		/// <param name="imageBrush">The ImageBrush containing all the image properties (UIImage, Stretch, AlignmentX, etc.)</param>
		/// <param name="fillMask">Optional mask layer (for when we use rounded corners)</param>
		private static void CreateImageBrushLayers(CGRect fullArea, CGRect insideArea, CALayer layer, List<CALayer> sublayers, ref int insertionIndex, ImageBrush imageBrush, CAShapeLayer fillMask)
		{
			var uiImage = imageBrush.ImageSource.ImageData;

			// This layer is the one we apply the mask on. It's the full size of the shape because the mask is as well.
			var imageContainerLayer = new CALayer
			{
				Frame = fullArea,
				Mask = fillMask,
				BackgroundColor = new CGColor(0, 0, 0, 0),
				MasksToBounds = true,
			};

			var imageFrame = imageBrush.GetArrangedImageRect(uiImage.Size, insideArea).ToCGRect();

			// This is the layer with the actual image in it. Its frame is the inside of the border.
			var imageLayer = new CALayer
			{
				Contents = uiImage.CGImage,
				Frame = imageFrame,
				MasksToBounds = true,
			};

			imageContainerLayer.AddSublayer(imageLayer);
			sublayers.Add(imageLayer);

			layer.InsertSublayer(imageContainerLayer, insertionIndex++);
			sublayers.Add(imageContainerLayer);
		}

		/// <summary>
		/// Creates and add 2 layers for LinearGradientBrush background
		/// </summary>
		/// <param name="fullArea">The full area available (includes the border)</param>
		/// <param name="insideArea">The "inside" of the border (excludes the border)</param>
		/// <param name="layer">The layer in which the gradient layers will be added</param>
		/// <param name="sublayers">List of layers to keep all references</param>
		/// <param name="insertionIndex">Where in the layer the new layers will be added</param>
		/// <param name="linearGradientBrush">The LinearGradientBrush</param>
		/// <param name="fillMask">Optional mask layer (for when we use rounded corners)</param>
		private static void CreateLinearGradientBrushLayers(CGRect fullArea, CGRect insideArea, CALayer layer, List<CALayer> sublayers, ref int insertionIndex, LinearGradientBrush linearGradientBrush, CAShapeLayer fillMask)
		{
			// This layer is the one we apply the mask on. It's the full size of the shape because the mask is as well.
			var gradientContainerLayer = new CALayer
			{
				Frame = fullArea,
				Mask = fillMask,
				BackgroundColor = new CGColor(0, 0, 0, 0),
				MasksToBounds = true,
			};

			var gradientFrame = new CGRect(new CGPoint(insideArea.X, insideArea.Y), insideArea.Size);

			// This is the layer with the actual gradient in it. Its frame is the inside of the border.
			var gradientLayer = linearGradientBrush.GetLayer(insideArea.Size);
			gradientLayer.Frame = gradientFrame;
			gradientLayer.MasksToBounds = true;

			gradientContainerLayer.AddSublayer(gradientLayer);
			sublayers.Add(gradientLayer);

			layer.InsertSublayer(gradientContainerLayer, insertionIndex++);
			sublayers.Add(gradientContainerLayer);
		}

		private class LayoutState : IEquatable<LayoutState>
		{
			public readonly CGRect Area;
			public readonly Brush Background;
			public readonly Brush BorderBrush;
			public readonly Thickness BorderThickness;
			public readonly CornerRadius CornerRadius;
			public readonly _Image BackgroundImage;

			internal CGPath BoundsPath { get; set; }

			public LayoutState(CGRect area, Brush background, Thickness borderThickness, Brush borderBrush, CornerRadius cornerRadius, _Image backgroundImage)
			{
				Area = area;
				Background = background;
				BorderBrush = borderBrush;
				CornerRadius = cornerRadius;
				BorderThickness = borderThickness;
				BackgroundImage = backgroundImage;
			}

			public override int GetHashCode() => Background?.GetHashCode() ?? 0 + BorderBrush?.GetHashCode() ?? 0;

			public override bool Equals(object obj) => Equals(obj as LayoutState);

			public bool Equals(LayoutState other) =>
				other != null
				&& other.Area == Area
				&& (other.Background?.Equals(Background) ?? false)
				&& (other.BorderBrush?.Equals(BorderBrush) ?? false)
				&& other.BorderThickness == BorderThickness
				&& other.CornerRadius == CornerRadius
				&& other.BackgroundImage == BackgroundImage;
		}
	}
}
