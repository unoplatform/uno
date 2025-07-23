using System.Linq;
using Android.Graphics;
using Uno.Extensions;
using Uno.UI;
using Microsoft.UI.Xaml.Media;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;
using Rect = Windows.Foundation.Rect;

namespace Microsoft.UI.Xaml.Media
{
	partial class RadialGradientBrush
	{
		private protected override void ApplyToPaintInner(Rect destinationRect, Paint paint)
		{
			paint.SetShader(GetShader(destinationRect.Size));
			paint.SetStyle(Paint.Style.Stroke);
		}

		internal Shader GetShader(Size size)
		{
			var center = Center;
			var radiusX = RadiusX;
			var radiusY = RadiusY;

			float radius;

			if (MappingMode == BrushMappingMode.RelativeToBoundingBox)
			{
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

			var colors = GradientStops.SelectToArray(s => ((AColor)GetColorWithOpacity(s.Color)).ToArgb());
			var locations = GradientStops.SelectToArray(s => (float)s.Offset);

			var transform = RelativeTransform?.ToNativeMatrix(size: size);

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
