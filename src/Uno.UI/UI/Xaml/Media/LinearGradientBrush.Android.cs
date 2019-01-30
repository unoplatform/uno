using System;
using System.Collections.Generic;
using System.Text;
using Android.Graphics;
using System.Linq;
using System.Drawing;

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
				var colors = GradientStops.Select(s => ((Android.Graphics.Color)s.Color).ToArgb()).ToArray();
				var locations = GradientStops.Select(s => (float)s.Offset).ToArray();

				var width = destinationRect.Width;
				var height = destinationRect.Height;

				var transform = RelativeTransform?.ToNative(size: new Windows.Foundation.Size(width, height), isBrush: true);

				//Matrix .MapPoints takes an array of floats
				var pts = new[] { StartPoint, EndPoint }
					.Select(p => new float[] { (float)(p.X * width), (float)(p.Y * height) })
					.SelectMany(p => p)
					.ToArray();

				transform?.MapPoints(pts);


				var shader = new LinearGradient(pts[0], pts[1], pts[2], pts[3], colors, locations, Shader.TileMode.Mirror);

				paint.SetShader(shader);
			}

			return paint;
		}
	}
}
