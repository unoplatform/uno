using System;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Media
{
	partial class RadialGradientBrush
	{
		internal string ToCssString(Size size)
		{
			var center = Center;
			var radiusX = RadiusX;
			var radiusY = RadiusY;

			if (MappingMode != BrushMappingMode.RelativeToBoundingBox)
			{
				center = new Point(center.X * size.Width, Center.Y * size.Height);
				radiusX = radiusX * size.Width;
				radiusY = radiusY * size.Height;
			}

			var stops = string.Join(
				",",
				GradientStops.Select(p => $"{GetColorWithOpacity(p.Color).ToHexString()} {(p.Offset * 100).ToStringInvariant()}%"));

			return $"radial-gradient(ellipse farthest-side at {radiusX * 100d}% {radiusY * 100d}%, {stops})";
		}

		internal UIElement ToSvgElement()
		{
			var center = Center;

			var radiusX = RadiusX;
			var radiusY = RadiusY;

			if (MappingMode != BrushMappingMode.RelativeToBoundingBox)
			{
				// TODO: not supported right now
			}

			var linearGradient = new SvgElement("radialGradient");

			// TODO: support ellipse shaped radial on SVG using a gradientTransform.
			var radius = (radiusX + radiusY) / 2d;

			linearGradient.SetAttribute(
				("cx", center.X.ToStringInvariant()),
				("cy", center.Y.ToStringInvariant()),
				("r", radius.ToStringInvariant())
			);

			var stops = GradientStops
				.Select(stop => $"<stop offset=\"{stop.Offset.ToStringInvariant()}\" style=\"stop-color:{stop.Color.ToHexString()}\" />");

			linearGradient.SetHtmlContent(string.Join(Environment.NewLine, stops));

			return linearGradient;
		}
	}
}
