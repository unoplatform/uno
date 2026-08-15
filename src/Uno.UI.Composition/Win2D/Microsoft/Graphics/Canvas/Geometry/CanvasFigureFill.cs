namespace Microsoft.Graphics.Canvas.Geometry;

/// <summary>
/// Specifies whether a figure contributes to the filled region of a geometry.
/// </summary>
public enum CanvasFigureFill
{
	/// <summary>
	/// The figure is filled.
	/// </summary>
	Default = 0,

	/// <summary>
	/// The figure is not filled and does not affect the fill of the surrounding geometry.
	/// </summary>
	DoesNotAffectFills = 1
}
