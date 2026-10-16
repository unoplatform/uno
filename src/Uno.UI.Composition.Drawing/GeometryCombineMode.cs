#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>Boolean combination modes for <see cref="IGeometry.Combine"/>.</summary>
public enum GeometryCombineMode
{
	Union,
	Intersect,
	Difference,
	Xor,
}
