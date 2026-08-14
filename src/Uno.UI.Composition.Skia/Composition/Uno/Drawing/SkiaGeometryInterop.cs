#nullable enable

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Bridges a backend-neutral <see cref="IGeometry"/> to the <see cref="SKPath"/> the Skia canvas needs. A native
/// <see cref="SkiaGeometrySource2D"/> is passed through (borrowed, never disposed); any other (foreign) geometry is
/// rebuilt into a transient SKPath from its neutral <see cref="IGeometry.StreamSegments"/> readback — so the Skia
/// backend knows only its own path type and the neutral contract, never a foreign concrete geometry type.
/// </summary>
internal static class SkiaGeometryInterop
{
	/// <summary>Borrows an <see cref="SKPath"/> for <paramref name="geometry"/>; dispose the lease when done.</summary>
	public static SkiaPathLease Lease(IGeometry geometry)
		=> geometry is SkiaGeometrySource2D skia
			? new SkiaPathLease(skia.Geometry, owned: false)
			: new SkiaPathLease(BuildForeign(geometry), owned: true);

	/// <summary>
	/// Returns an <see cref="SKPath"/> for <paramref name="geometry"/> suitable for retaining beyond the current
	/// scope (e.g. a native-element clip handed to a windowing API): a native geometry's path is returned directly
	/// (borrowed, do not dispose), a foreign geometry is rebuilt into a fresh path. Use <see cref="Lease"/> instead
	/// when the path is only needed within the current scope.
	/// </summary>
	public static SKPath ToSKPath(IGeometry geometry)
		=> geometry is SkiaGeometrySource2D skia ? skia.Geometry : BuildForeign(geometry);

	/// <summary>Builds a fresh <see cref="SKPath"/> from a foreign geometry's curve-preserving segment readback.</summary>
	private static SKPath BuildForeign(IGeometry geometry)
	{
		var builder = new SKPathBuilder
		{
			FillType = geometry.FillRule == GeometryFillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding,
		};

		geometry.StreamSegments(new SkiaPathSink(builder));
		return builder.Detach();
	}

	/// <summary>Forwards neutral path segments into an <see cref="SKPathBuilder"/>.</summary>
	private sealed class SkiaPathSink : IGeometrySink
	{
		private readonly SKPathBuilder _builder;

		public SkiaPathSink(SKPathBuilder builder) => _builder = builder;

		public void BeginFigure(Vector2 start) => _builder.MoveTo(new SKPoint(start.X, start.Y));

		public void LineTo(Vector2 point) => _builder.LineTo(new SKPoint(point.X, point.Y));

		public void QuadTo(Vector2 control, Vector2 point)
			=> _builder.QuadTo(new SKPoint(control.X, control.Y), new SKPoint(point.X, point.Y));

		public void CubicTo(Vector2 control1, Vector2 control2, Vector2 point)
			=> _builder.CubicTo(new SKPoint(control1.X, control1.Y), new SKPoint(control2.X, control2.Y), new SKPoint(point.X, point.Y));

		public void EndFigure(bool closed)
		{
			if (closed)
			{
				_builder.Close();
			}
		}
	}
}

/// <summary>A borrowed or owned <see cref="SKPath"/>; disposing releases it only when owned.</summary>
internal readonly ref struct SkiaPathLease
{
	private readonly bool _owned;

	public SkiaPathLease(SKPath path, bool owned)
	{
		Path = path;
		_owned = owned;
	}

	public SKPath Path { get; }

	public void Dispose()
	{
		if (_owned)
		{
			Path.Dispose();
		}
	}
}
