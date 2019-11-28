using System;
using System.Linq;
using Windows.Foundation;
using Uno.Extensions;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	public partial class LinearGradientBrush
	{
		internal string ToCssString(Size size)
		{
			var startPoint = StartPoint;
			var endPoint = EndPoint;

			var xDiff = (endPoint.X * size.Width) - (startPoint.X * size.Width);
			var yDiff = (startPoint.Y * size.Height) - (endPoint.Y * size.Height);

			var angle = Math.Atan2(xDiff, yDiff);

			var stops = string.Join(
				",",
				GradientStops.Select(p => $"{GetColorWithOpacity(p.Color).ToCssString()} {(p.Offset * 100).ToStringInvariant()}%"));

			return $"linear-gradient({angle}rad,{stops})";
		}

		/// <summary>
		/// Generates a linearGradient element that can be used inside SVG-based views (Path, etc)
		/// </summary>
		internal UIElement ToSvgElement()
		{
			var linearGradient = new SvgElement("linearGradient");

			linearGradient.SetAttribute(
				("x1", StartPoint.X.ToStringInvariant()),
				("y1", StartPoint.Y.ToStringInvariant()),
				("x2", EndPoint.X.ToStringInvariant()),
				("y2", EndPoint.Y.ToStringInvariant())
			);

			var stops = GradientStops.Select(stop => $"<stop offset=\"{stop.Offset.ToStringInvariant()}\" style=\"stop-color:{stop.Color.ToCssString()}\" />");

			linearGradient.SetHtmlContent(string.Join(Environment.NewLine, stops));

			return linearGradient;
		}
	}
}
