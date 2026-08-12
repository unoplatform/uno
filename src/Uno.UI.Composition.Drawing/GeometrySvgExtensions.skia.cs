#nullable enable

using System.Globalization;
using System.Numerics;
using System.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Neutral <see cref="IGeometry"/> → SVG path-data conversion for native-element clipping. Hosts previously reached
/// into the Skia backend (SkiaGeometryInterop → SKPath.ToSvgPathData); this keeps the host backend-agnostic — the
/// geometry streams its flattened contours through the neutral seam and we emit `M/L/Z` path data directly.
/// </summary>
public static class GeometrySvgExtensions
{
	/// <summary>Emits SVG path data (flattened polylines) for <paramref name="geometry"/> — Skia-free.</summary>
	public static string ToSvgPathData(this IGeometry geometry)
	{
		var sink = new SvgPathSink();
		geometry.StreamFlattened(sink);
		return sink.ToString();
	}

	private sealed class SvgPathSink : IFlattenedPathSink
	{
		private readonly StringBuilder _sb = new();

		public void BeginContour(Vector2 start)
			=> _sb.Append('M').Append(Fmt(start.X)).Append(' ').Append(Fmt(start.Y));

		public void LineTo(Vector2 point)
			=> _sb.Append('L').Append(Fmt(point.X)).Append(' ').Append(Fmt(point.Y));

		public void EndContour(bool closed)
		{
			if (closed)
			{
				_sb.Append('Z');
			}
		}

		private static string Fmt(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

		public override string ToString() => _sb.ToString();
	}
}
