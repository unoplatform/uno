using System;
using System.Linq;
using Windows.Foundation;
using Uno.Extensions;

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
	}
}
