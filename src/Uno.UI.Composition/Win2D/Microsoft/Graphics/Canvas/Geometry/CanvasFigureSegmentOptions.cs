using System;

namespace Microsoft.Graphics.Canvas.Geometry;

/// <summary>
/// Specifies how a segment is stroked and joined to the segment that follows it.
/// </summary>
[Flags]
internal enum CanvasFigureSegmentOptions
{
	/// <summary>
	/// The segment is stroked and joined using the stroke style in effect.
	/// </summary>
	None = 0,

	/// <summary>
	/// The segment is not stroked.
	/// </summary>
	ForceUnstroked = 1,

	/// <summary>
	/// The segment is always joined using a round line join, whatever the stroke style specifies.
	/// </summary>
	ForceRoundLineJoin = 2
}
