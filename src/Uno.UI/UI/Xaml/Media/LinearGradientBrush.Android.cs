using Android.Graphics;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Extensions;
using Point = Windows.Foundation.Point;
using Size = Windows.Foundation.Size;

namespace Windows.UI.Xaml.Media
{
	public partial class LinearGradientBrush
	{
		protected internal override Shader GetShader(Size size)
		{
			if (GradientStops.Count == 0)
			{
				return null;
			}

			var colors = GradientStops.SelectToList(s => ((Android.Graphics.Color)GetColorWithOpacity(s.Color)).ToArgb());
			var locations = GradientStops.SelectToList(s => (float)s.Offset);

			if (GradientStops.Count == 1)
			{
				// Android LinearGradient requires two ore more stop points.
				// We work around this by duplicating the first gradient stop.
				colors.Add(colors[0]);
				locations.Add(locations[0]);
			}

			var width = size.Width;
			var height = size.Height;

			Android.Graphics.Matrix nativeTransformMatrix = null;
			if (RelativeTransform != null)
			{
				var matrix = RelativeTransform.ToMatrix(Point.Zero, new Size(width, height));
				matrix.M31 *= (float)width;
				matrix.M32 *= (float)height;
				nativeTransformMatrix = matrix.ToNative();
			}

			//Matrix .MapPoints takes an array of floats
			var pts = MappingMode == BrushMappingMode.RelativeToBoundingBox
				? new[]
				{
						(float) (StartPoint.X * width),
						(float) (StartPoint.Y * height),
						(float) (EndPoint.X * width),
						(float) (EndPoint.Y * height)
				}
				: new[]
				{
						(float) ViewHelper.LogicalToPhysicalPixels(StartPoint.X),
						(float) ViewHelper.LogicalToPhysicalPixels(StartPoint.Y),
						(float) ViewHelper.LogicalToPhysicalPixels(EndPoint.X),
						(float) ViewHelper.LogicalToPhysicalPixels(EndPoint.Y)
				};

			nativeTransformMatrix?.MapPoints(pts);
			nativeTransformMatrix?.Dispose();
			return new LinearGradient(pts[0], pts[1], pts[2], pts[3], colors.ToArray(), locations.ToArray(), Shader.TileMode.Clamp);
		}
	}
}
