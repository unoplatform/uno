#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend-neutral handle to an immutable 2D geometry (path), produced by an <see cref="IDrawingBackend"/>
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
	/// subdivided). This is the neutral "geometry → segments" readback a backend renderer needs to
	/// tessellate/fill a path without knowing the concrete geometry type.
	/// </summary>
	void StreamFlattened(IFlattenedPathSink sink);
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
