#nullable enable

using System;
using Microsoft.UI.Composition;
using SkiaSharp;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Bridges a backend-neutral <see cref="IGeometry"/> to the <see cref="SKPath"/> the Skia canvas needs.
/// A native <see cref="SkiaGeometrySource2D"/> is passed through (borrowed, never disposed); a
/// <see cref="ManagedGeometry"/> is rebuilt into a transient SKPath the lease owns and disposes.
/// </summary>
internal static class SkiaGeometryInterop
{
	/// <summary>Borrows an <see cref="SKPath"/> for <paramref name="geometry"/>; dispose the lease when done.</summary>
	public static SkiaPathLease Lease(IGeometry geometry)
		=> geometry switch
		{
			SkiaGeometrySource2D skia => new SkiaPathLease(skia.Geometry, owned: false),
			ManagedGeometry managed => new SkiaPathLease(ToSKPath(managed), owned: true),
			_ => throw new NotSupportedException($"Cannot rasterize geometry of type {geometry.GetType().Name} with the Skia backend."),
		};

	/// <summary>
	/// Returns an <see cref="SKPath"/> for <paramref name="geometry"/> suitable for retaining beyond the
	/// current scope (e.g. a native-element clip handed to a windowing API): a native geometry's path is
	/// returned directly (borrowed, do not dispose), a managed geometry is rebuilt into a fresh path. Use
	/// <see cref="Lease"/> instead when the path is only needed within the current scope.
	/// </summary>
	public static SKPath ToSKPath(IGeometry geometry)
		=> geometry switch
		{
			SkiaGeometrySource2D skia => skia.Geometry,
			ManagedGeometry managed => ToSKPath(managed),
			_ => throw new NotSupportedException($"Cannot rasterize geometry of type {geometry.GetType().Name} with the Skia backend."),
		};

	/// <summary>Builds a fresh <see cref="SKPath"/> from a managed geometry's contours.</summary>
	public static SKPath ToSKPath(ManagedGeometry managed)
	{
		var builder = new SKPathBuilder
		{
			FillType = managed.FillRule == GeometryFillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding,
		};

		foreach (var contour in managed.Contours)
		{
			if (contour.Segments.Count == 0)
			{
				continue;
			}

			builder.MoveTo(new SKPoint(contour.Start.X, contour.Start.Y));
			foreach (var seg in contour.Segments)
			{
				if (seg.Kind == ManagedSegmentKind.Line)
				{
					builder.LineTo(new SKPoint(seg.End.X, seg.End.Y));
				}
				else
				{
					builder.CubicTo(
						new SKPoint(seg.C1.X, seg.C1.Y),
						new SKPoint(seg.C2.X, seg.C2.Y),
						new SKPoint(seg.End.X, seg.End.Y));
				}
			}

			if (contour.Closed)
			{
				builder.Close();
			}
		}

		return builder.Detach();
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
