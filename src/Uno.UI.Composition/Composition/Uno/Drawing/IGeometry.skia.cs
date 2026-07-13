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
internal interface IGeometry : IDisposable
{
	/// <summary>The loose (control-point) bounds of the geometry.</summary>
	Rect Bounds { get; }

	/// <summary>The tight (on-curve) bounds of the geometry.</summary>
	Rect TightBounds { get; }

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
}
