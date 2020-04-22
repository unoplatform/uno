using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using System.Linq;
using System.Drawing;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class LinearGradientBrush
	{

		protected override Paint GetPaintInner(Windows.Foundation.Rect destinationRect)
		{
			var paint = new Paint();

			// Android LinearGradient requires two ore more stop points.
			if (GradientStops.Count >= 2)
			{
				var colors = GradientStops.SelectToArray(s => ((Android.Graphics.Color)s.Color).ToArgb());
				var locations = GradientStops.SelectToArray(s => (float)s.Offset);

				var width = destinationRect.Width;
				var height = destinationRect.Height;

				var transform =
					RelativeTransform?.ToNative(size: new Windows.Foundation.Size(width, height), isBrush: true);

				//Matrix .MapPoints takes an array of floats
				var pts = MappingMode == BrushMappingMode.RelativeToBoundingBox
					? new []
						{
							(float)(StartPoint.X * width),
							(float)(StartPoint.Y * height),
							(float)(EndPoint.X * width),
							(float)(EndPoint.Y * height)
						}
					: new []
						{
							(float)StartPoint.X,
							(float)StartPoint.Y,
							(float)EndPoint.X,
							(float)EndPoint.Y
						};

				transform?.MapPoints(pts);
				var shader = new LinearGradient(pts[0], pts[1], pts[2], pts[3], colors, locations, Shader.TileMode.Clamp);

				paint.SetShader(shader);
			}

			return paint;
		}
	}
}
