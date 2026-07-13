#nullable enable

using Microsoft.UI.Composition;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend-neutral description of how a geometry is stroked, passed to
/// <see cref="IGeometry.GetStrokeFillGeometry"/>. Uses the WinUI composition stroke enums (which are
/// framework types, not backend types) so the contract is "give me the WinUI-correct fill region for
/// this stroke" — each backend produces it however it can.
/// </summary>
internal readonly struct StrokeStyle
{
	public float Thickness { get; init; }
	public CompositionStrokeCap StartCap { get; init; }
	public CompositionStrokeCap EndCap { get; init; }
	public CompositionStrokeCap DashCap { get; init; }
	public CompositionStrokeLineJoin LineJoin { get; init; }
	public float MiterLimit { get; init; }

	/// <summary>Dash intervals in multiples of <see cref="Thickness"/> (as authored), or null for a solid stroke.</summary>
	public float[]? DashArray { get; init; }
	public float DashOffset { get; init; }

	public float TrimStart { get; init; }
	public float TrimEnd { get; init; }
}
