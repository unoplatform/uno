#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition.Drawing;


namespace Microsoft.UI.Composition
{
	public partial class CompositionGeometry : CompositionObject
	{
		/// <summary>
		/// Kappa = (sqrt(2) - 1) * 4/3;
		//  Used to calculate bezier control points for each of the circle four arcs.
		//  - Approximating a 1/4 circle with a bezier curve.
		/// </summary>
		private const double CIRCLE_BEZIER_KAPPA = 0.552284749830793398402251632279597438092895833835930764235;

		internal static IGeometry BuildLineGeometry(Vector2 start, Vector2 end)
		{
			var builder = DrawingFactory.Current.CreatePathBuilder();

			builder.MoveTo(start);
			builder.LineTo(end);

			return builder.Build();
		}

		internal static IGeometry BuildRectangleGeometry(Vector2 offset, Vector2 size)
		{
			var builder = DrawingFactory.Current.CreatePathBuilder();

			// Top left
			builder.MoveTo(offset);
			// Top right
			builder.LineTo(offset + new Vector2(size.X, 0));
			// Bottom right
			builder.LineTo(offset + new Vector2(size.X, size.Y));
			// Bottom left
			builder.LineTo(offset + new Vector2(0, size.Y));
			// Top left
			builder.Close();

			return builder.Build();
		}

		internal static IGeometry BuildRoundedRectangleGeometry(Vector2 offset, Vector2 size, Vector2 cornerRadius)
		{
			float radiusX = Clamp(cornerRadius.X, 0, size.X * 0.5f);
			float radiusY = Clamp(cornerRadius.Y, 0, size.Y * 0.5f);

			float bezierX = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusX);
			float bezierY = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusY);

			var builder = DrawingFactory.Current.CreatePathBuilder();
			var lastPoint = new Vector2(offset.X + radiusX, offset.Y);

			builder.MoveTo(lastPoint);
			// Top line
			builder.LineTo(lastPoint + new Vector2(size.X - 2 * radiusX, 0));
			lastPoint += new Vector2(size.X - 2 * radiusX, 0);
			// Top-right Arc
			builder.CubicTo(
				lastPoint + new Vector2(radiusX - bezierX, 0),   // 1st control point
				lastPoint + new Vector2(radiusX, bezierY),       // 2nd control point
				lastPoint + new Vector2(radiusX, radiusY));      // End point
			lastPoint += new Vector2(radiusX, radiusY);

			// Right line
			builder.LineTo(lastPoint + new Vector2(0, size.Y - 2 * radiusY));
			lastPoint += new Vector2(0, size.Y - 2 * radiusY);
			// Bottom-right Arc
			builder.CubicTo(
				lastPoint + new Vector2(0, bezierY),             // 1st control point
				lastPoint + new Vector2(-bezierX, radiusY),      // 2nd control point
				lastPoint + new Vector2(-radiusX, radiusY));     // End point
			lastPoint += new Vector2(-radiusX, radiusY);

			// Bottom line
			builder.LineTo(lastPoint + new Vector2(-(size.X - 2 * radiusX), 0));
			lastPoint = lastPoint + new Vector2(-(size.X - 2 * radiusX), 0);
			// Bottom-left Arc
			builder.CubicTo(
				lastPoint + new Vector2(-radiusX + bezierX, 0),  // 1st control point
				lastPoint + new Vector2(-radiusX, -bezierY),     // 2nd control point
				lastPoint + new Vector2(-radiusX, -radiusY));    // End point
			lastPoint += new Vector2(-radiusX, -radiusY);

			// Left line
			builder.LineTo(lastPoint + new Vector2(0, -(size.Y - 2 * radiusY)));
			lastPoint += new Vector2(0, -(size.Y - 2 * radiusY));
			// Top-left Arc
			builder.CubicTo(
				lastPoint + new Vector2(0, -radiusY + bezierY),  // 1st control point
				lastPoint + new Vector2(bezierX, -radiusY),      // 2nd control point
				lastPoint + new Vector2(radiusX, -radiusY));     // End point

			builder.Close();

			return builder.Build();
		}

		internal static IGeometry BuildEllipseGeometry(Vector2 center, Vector2 radius)
		{
			float left = center.X - radius.X;
			float top = center.Y - radius.Y;
			float right = center.X + radius.X;
			float bottom = center.Y + radius.Y;

			float bezierX = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radius.X);
			float bezierY = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radius.Y);

			// IMPORTANT:
			// - The order of following operations is important for dashed strokes.
			// - Stroke might get merged in the end.
			// - WPF starts with bottom right ellipse arc.
			// - TODO: Verify UWP behavior

			var builder = DrawingFactory.Current.CreatePathBuilder();

			builder.MoveTo(new Vector2(right, top + radius.Y));
			// Bottom-right Arc
			builder.CubicTo(
				new Vector2(right, bottom - bezierY),  // 1st control point
				new Vector2(right - bezierX, bottom),  // 2nd control point
				new Vector2(right - radius.X, bottom)); // End point

			// Bottom-left Arc
			builder.CubicTo(
				new Vector2(left + bezierX, bottom),      // 1st control point
				new Vector2(left, bottom - bezierY),      // 2nd control point
				new Vector2(left, bottom - radius.Y));     // End point

			// Top-left Arc
			builder.CubicTo(
				new Vector2(left, top + bezierY),           // 1st control point
				new Vector2(left + bezierX, top),           // 2nd control point
				new Vector2(left + radius.X, top));          // End point

			// Top-right Arc
			builder.CubicTo(
				new Vector2(right - bezierX, top),       // 1st control point
				new Vector2(right, top + bezierY),       // 2nd control point
				new Vector2(right, top + radius.Y));      // End point

			builder.Close();

			return builder.Build();
		}

		private static float Clamp(float value, float minValue, float maxValue)
		{
			return Math.Min(Math.Max(Math.Abs(value), minValue), maxValue);
		}
	}
}
