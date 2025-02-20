using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using CoreAnimation;
using CoreGraphics;
using Uno.Extensions;
using ObjCRuntime;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Media
{
	partial class RadialGradientBrush
	{
		internal CALayer GetLayer(CGSize size)
		{
			var center = Center;
			var radiusX = RadiusX;
			var radiusY = RadiusY;

			var radius = (nfloat)(radiusX + radiusY) / 2.0f;

			var transform = RelativeTransform;

			var isRelative = MappingMode == BrushMappingMode.RelativeToBoundingBox;

			var colors = GradientStops.SelectToArray(gs => (CGColor)GetColorWithOpacity(gs.Color));
			var locations = GradientStops.SelectToArray(gs => new nfloat(gs.Offset));

			var layer = new RadialGradientLayer(colors, locations, center, radius, isRelative);
			layer.SetNeedsDisplay();
			return layer;
		}

		private class RadialGradientLayer : CALayer
		{
			private readonly CGColor[] _colors;
			private readonly nfloat[] _locations;
			private readonly CGPoint _center;
			private readonly nfloat _radius;
			private readonly bool _isRelative;

			public RadialGradientLayer(CGColor[] colors, nfloat[] locations, CGPoint center, nfloat radius, bool isRelative)
			{
				_colors = colors;
				_locations = locations;
				_center = center;
				_radius = radius;
				_isRelative = isRelative;
			}

			public override void DrawInContext(CGContext ctx)
			{
				var size = Frame.Size;
				Point center;
				nfloat radius;

				if (_isRelative)
				{
					center = new Point(_center.X * size.Width, _center.Y * size.Height);
					radius = (nfloat)(_radius * size.Width + _radius * size.Height) / 2.0f; // We take the avg
				}
				else
				{
					center = _center;
					radius = _radius;
				}

				var gradient = new CGGradient(CGColorSpace.CreateDeviceRGB(), _colors, _locations);

				var startCenter = new CGPoint(center.X, center.Y);
				ctx.DrawRadialGradient(gradient, startCenter, 0, startCenter, radius, CGGradientDrawingOptions.DrawsAfterEndLocation);

			}
		}
	}
}
