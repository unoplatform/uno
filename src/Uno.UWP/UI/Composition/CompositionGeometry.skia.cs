#nullable enable

using System;
using System.Numerics;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class CompositionGeometry : CompositionObject
	{
		/// <summary>
		/// Kappa = (sqrt(2) - 1) * 4/3;
		//  Used to calculate bezier control points for each of the circle four arcs. 
		//  - Approximating a 1/4 circle with a bezier curve.
		/// </summary>
		private const double CIRCLE_BEZIER_KAPPA = 0.552284749830793398402251632279597438092895833835930764235;

		internal static SKPath BuildLineGeometry(Vector2 start, Vector2 end)
		{
			SKPath path = new SKPath();

			path.MoveTo(start.ToSKPoint());
			path.LineTo(end.ToSKPoint());

			return path;
		}

		internal static SKPath BuildRectangleGeometry(Vector2 offset, Vector2 size)
		{
			SKPath path = new SKPath();

			// Top left
			path.MoveTo(new SKPoint(offset.X, offset.Y));
			// Top right
			path.RLineTo(new SKPoint(size.X, 0));
			// Bottom right
			path.RLineTo(new SKPoint(0, size.Y));
			// Bottom left
			path.RLineTo(new SKPoint(-size.X, 0));
			// Top left
			path.Close();

			return path;
		}

		internal static SKPath BuildRoundedRectangleGeometry(Vector2 offset, Vector2 size, Vector2 cornerRadius)
		{
			float radiusX = Clamp(cornerRadius.X, 0, size.X * 0.5f);
			float radiusY = Clamp(cornerRadius.Y, 0, size.Y * 0.5f);

			float bezierX = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusX);
			float bezierY = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusY);

			SKPath path = new SKPath();
			var lastPoint = new SKPoint(offset.X + radiusX, offset.Y);

			path.MoveTo(lastPoint);
			// Top line
			path.LineTo(lastPoint + new SKPoint(size.X - 2 * radiusX, 0));
			lastPoint += new SKPoint(size.X - 2 * radiusX, 0);
			// Top-right Arc
			path.CubicTo(
				lastPoint + new SKPoint(radiusX - bezierX, 0),   // 1st control point
				lastPoint + new SKPoint(radiusX, bezierY),       // 2nd control point
				lastPoint + new SKPoint(radiusX, radiusY));      // End point
			lastPoint += new SKPoint(radiusX, radiusY);

			// Right line
			path.LineTo(lastPoint + new SKPoint(0, size.Y - 2 * radiusY));
			lastPoint += new SKPoint(0, size.Y - 2 * radiusY);
			// Bottom-right Arc
			path.CubicTo(
				lastPoint + new SKPoint(0, bezierY),             // 1st control point
				lastPoint + new SKPoint(-bezierX, radiusY),      // 2nd control point
				lastPoint + new SKPoint(-radiusX, radiusY));     // End point
			lastPoint += new SKPoint(-radiusX, radiusY);

			// Bottom line
			path.LineTo(lastPoint + new SKPoint(-(size.X - 2 * radiusX), 0));
			lastPoint = lastPoint + new SKPoint(-(size.X - 2 * radiusX), 0);
			// Bottom-left Arc
			path.CubicTo(
				lastPoint + new SKPoint(-radiusX + bezierX, 0),  // 1st control point
				lastPoint + new SKPoint(-radiusX, -bezierY),     // 2nd control point
				lastPoint + new SKPoint(-radiusX, -radiusY));    // End point
			lastPoint += new SKPoint(-radiusX, -radiusY);

			// Left line
			path.LineTo(lastPoint + new SKPoint(0, -(size.Y - 2 * radiusY)));
			lastPoint += new SKPoint(0, -(size.Y - 2 * radiusY));
			// Top-left Arc
			path.CubicTo(
				lastPoint + new SKPoint(0, -radiusY + bezierY),  // 1st control point
				lastPoint + new SKPoint(bezierX, -radiusY),      // 2nd control point
				lastPoint + new SKPoint(radiusX, -radiusY));     // End point

			path.Close();

			return path;
		}

		internal static SKPath BuildEllipseGeometry(Vector2 center, Vector2 radius)
		{
			SKRect rect = SKRect.Create(center.X - radius.X, center.Y - radius.Y, radius.X * 2, radius.Y * 2);

			float bezierX = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radius.X);
			float bezierY = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radius.Y);

			// IMPORTANT:
			// - The order of following operations is important for dashed strokes.
			// - Stroke might get merged in the end.
			// - WPF starts with bottom right ellipse arc.
			// - TODO: Verify UWP behavior

			SKPath path = new SKPath();

			path.MoveTo(new SKPoint(rect.Right, rect.Top + radius.Y));
			// Bottom-right Arc
			path.CubicTo(
				new SKPoint(rect.Right, rect.Bottom - bezierY),  // 1st control point
				new SKPoint(rect.Right - bezierX, rect.Bottom),  // 2nd control point
				new SKPoint(rect.Right - radius.X, rect.Bottom)); // End point

			// Bottom-left Arc
			path.CubicTo(
				new SKPoint(rect.Left + bezierX, rect.Bottom),      // 1st control point
				new SKPoint(rect.Left, rect.Bottom - bezierY),      // 2nd control point
				new SKPoint(rect.Left, rect.Bottom - radius.Y));     // End point

			// Top-left Arc
			path.CubicTo(
				new SKPoint(rect.Left, rect.Top + bezierY),           // 1st control point
				new SKPoint(rect.Left + bezierX, rect.Top),           // 2nd control point
				new SKPoint(rect.Left + radius.X, rect.Top));          // End point

			// Top-right Arc
			path.CubicTo(
				new SKPoint(rect.Right - bezierX, rect.Top),       // 1st control point
				new SKPoint(rect.Right, rect.Top + bezierY),       // 2nd control point
				new SKPoint(rect.Right, rect.Top + radius.Y));      // End point

			path.Close();

			return path;
		}

		private static float Clamp(float value, float minValue, float maxValue)
		{
			return Math.Min(Math.Max(Math.Abs(value), minValue), maxValue);
		}
	}
}
