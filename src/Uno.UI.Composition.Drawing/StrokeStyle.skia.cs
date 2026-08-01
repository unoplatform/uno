#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend-neutral description of how a geometry is stroked, passed to
/// <see cref="IGeometry.GetStrokeFillGeometry"/>. Each backend produces the WinUI-correct fill region
/// for this stroke however it can (e.g. simulating <see cref="StrokeCap.Triangle"/> with custom geometry).
/// </summary>
public readonly struct StrokeStyle
{
	public float Thickness { get; init; }
	public StrokeCap StartCap { get; init; }
	public StrokeCap EndCap { get; init; }
	public StrokeCap DashCap { get; init; }
	public StrokeJoin LineJoin { get; init; }
	public float MiterLimit { get; init; }

	/// <summary>Dash intervals in multiples of <see cref="Thickness"/> (as authored), or null for a solid stroke.</summary>
	public float[]? DashArray { get; init; }
	public float DashOffset { get; init; }

	public float TrimStart { get; init; }
	public float TrimEnd { get; init; }
}
