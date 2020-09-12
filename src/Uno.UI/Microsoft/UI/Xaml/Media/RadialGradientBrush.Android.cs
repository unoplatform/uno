using System.Linq;
using Android.Graphics;
using Uno.Extensions;
using Uno.UI;
using Windows.UI.Xaml.Media;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;

namespace Microsoft.UI.Xaml.Media
{
	partial class RadialGradientBrush
	{
		protected internal override Shader GetShader(Rect destinationRect)
		{
			var center = Center;
			var radiusX = RadiusX;
			var radiusY = RadiusY;

			float radius;

			if (MappingMode == BrushMappingMode.RelativeToBoundingBox)
			{
				var size = destinationRect.Size;

				center = new Point(center.X * size.Width, Center.Y * size.Height);
				radius = (float)(radiusX * size.Width + radiusY * size.Height) / 2.0f; // We take the avg
			}
			else
			{
				center = center.LogicalToPhysicalPixels();
				radius = ViewHelper.LogicalToPhysicalPixels((radiusX + radiusY) / 2.0d); // We take the avg
			}

			// Android requires a radius and two or more stop points.
			if (radius <= 0 || GradientStops.Count < 2)
			{
				return null;
			}

			var colors = GradientStops.SelectToArray(s => ((Android.Graphics.Color)s.Color).ToArgb());
			var locations = GradientStops.SelectToArray(s => (float)s.Offset);

			var width = destinationRect.Width;
			var height = destinationRect.Height;

			var transform = RelativeTransform?.ToNative(size: new Windows.Foundation.Size(width, height), isBrush: true);

			var shader = new RadialGradient(
				(float)center.X,
				(float)center.Y,
				radius,
				colors,
				locations,
				Shader.TileMode.Clamp);

			return shader;
		}
	}
}
