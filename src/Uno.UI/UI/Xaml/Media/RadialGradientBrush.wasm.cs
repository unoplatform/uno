using System;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	partial class RadialGradientBrush
	{
		internal override string ToCssString(Size size)
		{
			var center = Center;

			var radiusX = RadiusX;
			var radiusY = RadiusY;

			var ratio = size.AspectRatio();

			if (MappingMode != BrushMappingMode.RelativeToBoundingBox)
			{
				center = new Point(center.X * size.Width, Center.Y * size.Height);
				radiusX = radiusX * size.Width;
				radiusY = radiusY * size.Height;
			}

			var stops = string.Join(
				",",
				GradientStops.Select(p => $"{GetColorWithOpacity(p.Color).ToHexString()} {(p.Offset * 100).ToStringInvariant()}%"));

			return $"radial-gradient(ellipse farthest-side at {radiusX * 50d}% {radiusY * 50d}%, {stops})";
		}

		internal override UIElement ToSvgElement()
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
			var radius = (radiusX + radiusY) / 4;

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
