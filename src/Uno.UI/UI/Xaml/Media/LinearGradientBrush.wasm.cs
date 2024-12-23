using System;
using System.Linq;
using Windows.Foundation;
using Uno.Extensions;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media;

partial class LinearGradientBrush
{
	internal override string ToCssString(Size size)
	{
		var startPoint = StartPoint;
		var endPoint = EndPoint;

		if (MappingMode != BrushMappingMode.RelativeToBoundingBox)
		{
			startPoint = new Point(startPoint.X * size.Width, startPoint.Y * size.Height);
			endPoint = new Point(endPoint.X * size.Width, endPoint.Y * size.Height);
		}

		var xDiff = endPoint.X - startPoint.X;
		var yDiff = startPoint.Y - endPoint.Y;

		var angle = Math.Atan2(xDiff, yDiff).ToStringInvariant();

		var stops = string.Join(
			",",
			GradientStops.Select(p => $"{GetColorWithOpacity(p.Color).ToHexString()} {(p.Offset * 100).ToStringInvariant()}%"));

		return $"linear-gradient({angle}rad,{stops})";
	}

	/// <summary>
	/// Generates a linearGradient element that can be used inside SVG-based views (Path, etc)
	/// </summary>
	internal override UIElement ToSvgElement()
	{
		var linearGradient = new SvgElement("linearGradient");

		if (MappingMode != BrushMappingMode.RelativeToBoundingBox)
		{
			// Not supported yet
		}

		linearGradient.SetAttribute(
			("x1", StartPoint.X.ToStringInvariant()),
			("y1", StartPoint.Y.ToStringInvariant()),
			("x2", EndPoint.X.ToStringInvariant()),
			("y2", EndPoint.Y.ToStringInvariant())
		);

		var stops = GradientStops.Select(stop => $"<stop offset=\"{stop.Offset.ToStringInvariant()}\" style=\"stop-color:{stop.Color.ToHexString()}\" />");

		linearGradient.SetHtmlContent(string.Join(Environment.NewLine, stops));

		return linearGradient;
	}
}
