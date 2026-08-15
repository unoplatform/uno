namespace Microsoft.Graphics.Canvas.Geometry;

/// <summary>
/// Indicates whether a figure is open or closed.
/// </summary>
public enum CanvasFigureLoop
{
	/// <summary>
	/// The figure is open: its end point is not connected to its start point.
	/// </summary>
	Open = 0,

	/// <summary>
	/// The figure is closed: its end point is connected back to its start point.
	/// </summary>
	Closed = 1
}
