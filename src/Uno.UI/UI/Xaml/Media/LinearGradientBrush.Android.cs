using Android.Graphics;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Extensions;
using Rect = Windows.Foundation.Rect;

namespace Windows.UI.Xaml.Media
{
	public partial class LinearGradientBrush
	{
		protected internal override Shader GetShader(Rect destinationRect)
		{
			// Android LinearGradient requires two ore more stop points.
			//TODO: If there is only one - we should use solid color brush here
			if (GradientStops.Count >= 2)
			{
				var colors = GradientStops.SelectToArray(s => ((Android.Graphics.Color)s.Color).ToArgb());
				var locations = GradientStops.SelectToArray(s => (float)s.Offset);

				var width = destinationRect.Width;
				var height = destinationRect.Height;

				Android.Graphics.Matrix nativeTransformMatrix = null;
				if (RelativeTransform != null)
				{
					var matrix = RelativeTransform.ToMatrix(Foundation.Point.Zero, new Windows.Foundation.Size(width, height));
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
				return new LinearGradient(pts[0], pts[1], pts[2], pts[3], colors, locations, Shader.TileMode.Clamp);
			}

			return null;
		}
	}
}
