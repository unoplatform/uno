using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using System.Linq;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreAnimation;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using MonoTouch.CoreAnimation;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
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
			// Background color is black by default, if and only if overriding Draw(CGRect rect).
			base.BackgroundColor = SolidColorBrushHelper.Transparent.Color;

			if (FeatureConfiguration.UIElement.UseLegacyClipping)
			{
				Layer.MasksToBounds = true;
			}

			Layer.AddSublayer(_rectangleLayer);

			_rectangleLayer.FillColor = UIColor.Clear.CGColor;
		}

		protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
		{
			// Don't call base, we need to keep UIView.BackgroundColor set to transparent
			// because we're overriding draw.
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

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
						? (new RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width))
						: (new RectangleF((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height));
					break;
				case Stretch.UniformToFill:
					area = (area.Height > area.Width)
						? (new RectangleF((float)area.X, (float)area.Y, (float)area.Height, (float)area.Height))
						: (new RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Width));
					break;
			}

			area = area.Shrink((float)ActualStrokeThickness / 2);

			if (Math.Max(RadiusX, RadiusY) > 0)
			{
				_rectangleLayer.Path = UIBezierPath.FromRoundedRect(area, UIRectCorner.AllCorners, new CGSize(RadiusX, RadiusY)).CGPath;
			}
			else
			{
				_rectangleLayer.Path = UIBezierPath.FromRect(area).CGPath;
			}

			if (_gradientLayer != null)
			{
				_gradientLayer.Frame = _rectangleLayer.Path.BoundingBox;
			}
		}

		protected override void OnStrokeUpdated(Brush newValue)
		{
			base.OnStrokeUpdated(newValue);

			var brush = Stroke as SolidColorBrush ?? SolidColorBrushHelper.Transparent;

			_strokeSubscription.Disposable =
				Brush.AssignAndObserveBrush(brush, c => _rectangleLayer.StrokeColor = c);

			SetNeedsDisplay();
		}

		protected override void OnFillChanged(Brush newValue)
		{
			base.OnFillChanged(newValue);

			if (_gradientLayer != null)
			{
				_gradientLayer.RemoveFromSuperLayer();
				_gradientLayer = null;
			}

			_rectangleLayer.FillColor = UIColor.Clear.CGColor;

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

			SetNeedsLayout();
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

			SetNeedsLayout();
		}

		protected override void OnStrokeDashArrayUpdated(DoubleCollection newValue)
		{
			base.OnStrokeDashArrayUpdated(newValue);

			_rectangleLayer.LineDashPattern = newValue.Safe().Select(d => new NSNumber(d)).ToArray();

			SetNeedsDisplay();
		}

		protected override void OnStretchUpdated(Stretch newValue)
		{
			base.OnStretchUpdated(newValue);

			SetNeedsLayout();
		}

		partial void OnRadiusXChangedPartial()
		{
			SetNeedsLayout();
		}

		partial void OnRadiusYChangedPartial()
		{
			SetNeedsLayout();
		}
	}
}
