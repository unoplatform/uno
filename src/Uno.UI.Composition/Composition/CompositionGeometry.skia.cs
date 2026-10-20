#nullable enable

using System;
using System.Numerics;
using Uno.UI.Composition.Drawing;


namespace Microsoft.UI.Composition
{
	public partial class CompositionGeometry : CompositionObject
	{
		internal enum EllipseFigureStart
		{
			Top,
			Right,
			Left,
		}

		/// <summary>
		/// Kappa = (sqrt(2) - 1) * 4/3;
		//  Used to calculate bezier control points for each of the circle four arcs.
		//  - Approximating a 1/4 circle with a bezier curve.
		/// </summary>
		private const double CIRCLE_BEZIER_KAPPA = 0.552284749830793398402251632279597438092895833835930764235;

		/// <summary>
		/// The same outline as <see cref="BuildGeometry"/>, starting where WinUI starts an untrimmed dash pattern, or null
		/// when that is the trim origin too.
		/// </summary>
		internal virtual IGeometry? BuildDashGeometry() => null;

		internal static IGeometry BuildLineGeometry(Vector2 start, Vector2 end)
		{
			var builder = GeometryFactory.Current.CreatePathBuilder();

			builder.MoveTo(start);
			builder.LineTo(end);

			return builder.Build();
		}

		internal static IGeometry BuildRectangleGeometry(Vector2 offset, Vector2 size)
		{
			var builder = GeometryFactory.Current.CreatePathBuilder();

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

		/// <param name="startAtTopLeftArc">Starts the figure where the top-left arc begins (WinUI's dash origin) instead of
		/// where it ends (the trim origin).</param>
		internal static IGeometry BuildRoundedRectangleGeometry(Vector2 offset, Vector2 size, Vector2 cornerRadius, bool startAtTopLeftArc = false)
		{
			float radiusX = Clamp(cornerRadius.X, 0, size.X * 0.5f);
			float radiusY = Clamp(cornerRadius.Y, 0, size.Y * 0.5f);

			float bezierX = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusX);
			float bezierY = (float)((1.0 - CIRCLE_BEZIER_KAPPA) * radiusY);

			var builder = GeometryFactory.Current.CreatePathBuilder();
			var topLeftArcEnd = new Vector2(offset.X + radiusX, offset.Y);
			var lastPoint = topLeftArcEnd;

			void AddTopLeftArc() => builder.CubicTo(
				new Vector2(offset.X, offset.Y + bezierY),
				new Vector2(offset.X + bezierX, offset.Y),
				topLeftArcEnd);

			if (startAtTopLeftArc)
			{
				builder.MoveTo(new Vector2(offset.X, offset.Y + radiusY));
				AddTopLeftArc();
			}
			else
			{
				builder.MoveTo(lastPoint);
			}

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
			if (!startAtTopLeftArc)
			{
				AddTopLeftArc();
			}

			builder.Close();

			return builder.Build();
		}

		internal static IGeometry BuildEllipseGeometry(Vector2 center, Vector2 radius, EllipseFigureStart start = EllipseFigureStart.Top)
		{
			// The figure winds clockwise. Composition trims measure from the top (12 o'clock, the After Effects/Lottie
			// convention) so trimmed arcs grow from the top as on WinUI, while WinUI dashes a Composition ellipse from
			// 9 o'clock and a XAML EllipseGeometry from 3 o'clock.
			var kx = (float)(CIRCLE_BEZIER_KAPPA * radius.X);
			var ky = (float)(CIRCLE_BEZIER_KAPPA * radius.Y);
			ReadOnlySpan<Vector2> anchors =
			[
				new(center.X, center.Y - radius.Y),
				new(center.X + radius.X, center.Y),
				new(center.X, center.Y + radius.Y),
				new(center.X - radius.X, center.Y),
			];
			ReadOnlySpan<Vector2> tangents = [new(kx, 0), new(0, ky), new(-kx, 0), new(0, -ky)];
			var first = start switch
			{
				EllipseFigureStart.Right => 1,
				EllipseFigureStart.Left => 3,
				_ => 0,
			};

			var builder = GeometryFactory.Current.CreatePathBuilder();
			builder.MoveTo(anchors[first]);
			for (var i = 0; i < 4; i++)
			{
				var from = (first + i) % 4;
				var to = (from + 1) % 4;
				builder.CubicTo(anchors[from] + tangents[from], anchors[to] - tangents[to], anchors[to]);
			}

			builder.Close();

			return builder.Build();
		}

		private static float Clamp(float value, float minValue, float maxValue)
		{
			return Math.Min(Math.Max(Math.Abs(value), minValue), maxValue);
		}
	}
}
