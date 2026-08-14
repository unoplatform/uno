#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend-neutral handle to an immutable 2D geometry (path), produced by an <see cref="IDrawingFactory"/>
/// and consumed by the drawing pipeline for filling, clipping, stroking and hit-testing.
/// </summary>
/// <remarks>
/// This is the "resource handle" half of the pluggable-backend abstraction: geometry is not a draw-time
/// value. It is queried (<see cref="Bounds"/>, <see cref="FillContains"/>), transformed and combined well
/// outside of any draw call — hit-testing, for instance, happens with no canvas at all — so it must cross
/// the backend boundary as a stateful object rather than as inline draw parameters.
/// </remarks>
public interface IGeometry : IDisposable
{
	/// <summary>The tight (on-curve) bounds of the geometry.</summary>
	Rect Bounds { get; }

	/// <summary>The fill rule a backend renderer must use when filling this geometry (even-odd vs winding).</summary>
	GeometryFillRule FillRule { get; }

	/// <summary>Whether the geometry contains no drawable area.</summary>
	bool IsEmpty { get; }

	/// <summary>Whether the filled interior of the geometry contains <paramref name="point"/>.</summary>
	bool FillContains(Vector2 point);

	/// <summary>Returns a new geometry with <paramref name="matrix"/> baked in.</summary>
	IGeometry Transform(Matrix3x2 matrix);

	/// <summary>Returns a new geometry combining this one and <paramref name="other"/> per <paramref name="mode"/>.</summary>
	IGeometry Combine(IGeometry other, GeometryCombineMode mode);

	/// <summary>Returns the fill region of this geometry, optionally trimmed to [<paramref name="trimStart"/>, <paramref name="trimEnd"/>].</summary>
	IGeometry GetFilledGeometry(float trimStart, float trimEnd);

	/// <summary>
	/// Returns the fill region produced by stroking this geometry with <paramref name="style"/>, matching
	/// WinUI stroke semantics (caps, miter-clip, dash caps). The caller-owned result must be disposed.
	/// </summary>
	IGeometry GetStrokeFillGeometry(in StrokeStyle style);

	/// <summary>
	/// Streams the geometry's outline to <paramref name="sink"/> as flattened polyline contours (curves
	/// subdivided at a fixed local tolerance). The readback a tessellating backend (e.g. WebGPU) needs to fill a
	/// path without a curve rasterizer. For a backend with its own rasterizer, prefer <see cref="StreamSegments"/>.
	/// </summary>
	void StreamFlattened(IFlattenedPathSink sink);

	/// <summary>
	/// Streams the geometry's outline to <paramref name="sink"/> as un-flattened path segments (curves preserved).
	/// Unlike <see cref="StreamFlattened"/>, this keeps the béziers so a backend with its own curve rasterizer can
	/// reconstruct a faithful native path and flatten at device resolution (smooth under any transform, correct
	/// strokes). It is the neutral "geometry → path" readback that lets any backend consume any geometry without
	/// knowing its concrete type. The fill rule travels separately on <see cref="FillRule"/>.
	/// </summary>
	void StreamSegments(IGeometrySink sink);
}

/// <summary>Receives flattened polyline contours from <see cref="IGeometry.StreamFlattened"/>.</summary>
public interface IFlattenedPathSink
{
	/// <summary>Starts a new contour at <paramref name="start"/>.</summary>
	void BeginContour(Vector2 start);

	/// <summary>Adds a straight edge to <paramref name="point"/> within the current contour.</summary>
	void LineTo(Vector2 point);

	/// <summary>Ends the current contour; <paramref name="closed"/> mirrors the source subpath's closed flag.</summary>
	void EndContour(bool closed);
}

/// <summary>Receives un-flattened path segments (curves preserved) from <see cref="IGeometry.StreamSegments"/>.</summary>
public interface IGeometrySink
{
	/// <summary>Starts a new figure at <paramref name="start"/>.</summary>
	void BeginFigure(Vector2 start);

	/// <summary>Adds a straight segment to <paramref name="point"/>.</summary>
	void LineTo(Vector2 point);

	/// <summary>Adds a quadratic bézier segment through <paramref name="control"/> to <paramref name="point"/>.</summary>
	void QuadTo(Vector2 control, Vector2 point);

	/// <summary>Adds a cubic bézier segment through <paramref name="control1"/>/<paramref name="control2"/> to <paramref name="point"/>.</summary>
	void CubicTo(Vector2 control1, Vector2 control2, Vector2 point);

	/// <summary>Ends the current figure; <paramref name="closed"/> mirrors the source subpath's closed flag.</summary>
	void EndFigure(bool closed);
}
