using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using System.Linq;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI;
using Foundation;
using Windows.Foundation;

#if XAMARIN_IOS_UNIFIED
using UIKit;
using CoreAnimation;
using CoreGraphics;
using _Color = UIKit.UIColor;
using _BezierPath = UIKit.UIBezierPath;
#elif __MACOS__
using AppKit;
using CoreAnimation;
using CoreGraphics;
using _Color = AppKit.NSColor;
using _BezierPath = AppKit.NSBezierPath;
#endif

namespace Windows.UI.Xaml.Shapes
{
	public partial class Rectangle
	{
		CAShapeLayer _rectangleLayer = new CAShapeLayer();
		CAGradientLayer _gradientLayer;
		SerialDisposable _fillSubscription = new SerialDisposable();
		SerialDisposable _strokeSubscription = new SerialDisposable();

		public Rectangle()
		{
			//Set default stretch value
			this.Stretch = Stretch.Fill;

#if __IOS__
			// Background color is black by default, if and only if overriding Draw(CGRect rect).
			base.BackgroundColor = SolidColorBrushHelper.Transparent.Color;
#else
			base.WantsLayer = true;
			base.Layer.BackgroundColor = SolidColorBrushHelper.Transparent.Color;
#endif

			Layer.AddSublayer(_rectangleLayer);

			_rectangleLayer.FillColor = _Color.Clear.CGColor;
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
			// because we're overriding draw.
		}

		protected override Foundation.Size ArrangeOverride(Foundation.Size finalSize)
		{
			var area = Bounds;

			switch (Stretch)
			{
				default:
				case Stretch.None:
					area = CGRect.Empty;
					break;
				case Stretch.Fill:
					area = Bounds;
					break;
				case Stretch.Uniform:
					area = (area.Height > area.Width)
						? (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width))
						: (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height));
					break;
				case Stretch.UniformToFill:
					area = (area.Height > area.Width)
						? (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height))
						: (new global::System.Drawing.RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width));
					break;
			}

			area = area.Shrink((float)ActualStrokeThickness / 2);

			if (Math.Max(RadiusX, RadiusY) > 0)
			{
#if __IOS__
				_rectangleLayer.Path = _BezierPath.FromRoundedRect(area, UIRectCorner.AllCorners, new CGSize(RadiusX, RadiusY)).CGPath;
#else
				_rectangleLayer.Path = _BezierPath.FromRoundedRect(area, (nfloat)RadiusX, (nfloat)RadiusY).ToCGPath();
#endif
			}
			else
			{
				_rectangleLayer.Path = _BezierPath.FromRect(area).ToCGPath();
			}

			if (_gradientLayer != null)
			{
				_gradientLayer.Frame = _rectangleLayer.Path.BoundingBox;
			}

			return base.ArrangeOverride(finalSize);
		}

		protected override void OnStrokeUpdated(Brush newValue)
		{
			base.OnStrokeUpdated(newValue);

			var brush = Stroke as SolidColorBrush ?? SolidColorBrushHelper.Transparent;

			_strokeSubscription.Disposable =
				Brush.AssignAndObserveBrush(brush, c => _rectangleLayer.StrokeColor = c);

			this.SetNeedsDisplay();
		}

		protected override void OnFillChanged(Brush newValue)
		{
			base.OnFillChanged(newValue);

			if (_gradientLayer != null)
			{
				_gradientLayer.RemoveFromSuperLayer();
				_gradientLayer = null;
			}

			_rectangleLayer.FillColor = _Color.Clear.CGColor;

			var scbFill = newValue as SolidColorBrush;
			var lgbFill = newValue as LinearGradientBrush;
			if (scbFill != null)
			{
				_fillSubscription.Disposable =
					Brush.AssignAndObserveBrush(scbFill, c => _rectangleLayer.FillColor = c);
			}
			else if (lgbFill != null)
			{
				_gradientLayer = lgbFill.GetLayer(Frame.Size);
				Layer.InsertSublayer(_gradientLayer, 0); // We want the _gradientLayer to be below the _rectangleLayer (which contains the stroke)
			}

			InvalidateMeasure();
		}

		protected override void OnStrokeThicknessUpdated(double newValue)
		{
			base.OnStrokeThicknessUpdated(newValue);

			if (newValue > 0 && newValue < ViewHelper.OnePixel)
			{
				StrokeThickness = (float)ViewHelper.OnePixel;
				return;
			}

			_rectangleLayer.LineWidth = (float)newValue;

			InvalidateMeasure();
		}

		protected override void OnStrokeDashArrayUpdated(DoubleCollection newValue)
		{
			base.OnStrokeDashArrayUpdated(newValue);

			_rectangleLayer.LineDashPattern = newValue.Safe().Select(d => new NSNumber(d)).ToArray();

			this.SetNeedsDisplay();
		}

		protected override void OnStretchUpdated(Stretch newValue)
		{
			base.OnStretchUpdated(newValue);

			InvalidateMeasure();
		}

		partial void OnRadiusXChangedPartial()
		{
			InvalidateMeasure();
		}

		partial void OnRadiusYChangedPartial()
		{
			InvalidateMeasure();
		}
	}
}
