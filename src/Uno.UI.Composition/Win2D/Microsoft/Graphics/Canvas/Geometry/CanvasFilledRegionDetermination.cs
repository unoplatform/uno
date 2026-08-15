namespace Microsoft.Graphics.Canvas.Geometry;

/// <summary>
/// Specifies how the intersecting areas of a geometry's figures are combined to form its filled region.
/// </summary>
public enum CanvasFilledRegionDetermination
{
	/// <summary>
	/// A point is inside the fill region when a ray cast from it to infinity crosses an odd number of
	/// path segments.
	/// </summary>
	Alternate = 0,

	/// <summary>
	/// A point is inside the fill region when the signed number of path-segment crossings along a ray
	/// cast from it to infinity is non-zero, counting left-to-right crossings as +1 and right-to-left
	/// crossings as -1.
	/// </summary>
	Winding = 1
}
