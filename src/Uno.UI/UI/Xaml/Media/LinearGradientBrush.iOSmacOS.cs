using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.Foundation;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class LinearGradientBrush
	{
		internal override CALayer GetLayer(CGSize size)
		{
			var gradientLayer = new CAGradientLayer
			{
				Colors = GradientStops.SelectToArray(gs => (CGColor)GetColorWithOpacity(gs.Color)),
				Locations = GradientStops.SelectToArray(gs => new NSNumber(gs.Offset))
			};
			var transform = RelativeTransform;

			var startPoint = transform?.TransformPoint(StartPoint) ?? StartPoint;
			var endPoint = transform?.TransformPoint(EndPoint) ?? EndPoint;

			var width = size.Width;
			var height = size.Height;

			if (transform != null)
			{
				var matrix = transform.ToMatrix(Point.Zero, new Size(width, height));
				matrix.M31 *= (float)width;
				matrix.M32 *= (float)height;

				startPoint = matrix.Transform(startPoint);
				endPoint = matrix.Transform(endPoint);
			}
			if (MappingMode == BrushMappingMode.Absolute)
			{
				startPoint = new Point(startPoint.X / size.Width, startPoint.Y / size.Height);
				endPoint = new Point(endPoint.X / size.Width, endPoint.Y / size.Height);
			}

			gradientLayer.StartPoint = startPoint;
			gradientLayer.EndPoint = endPoint;

			return gradientLayer;
		}
	}
}
