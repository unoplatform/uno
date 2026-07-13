#nullable enable

namespace Uno.UI.Composition.Drawing;

/// <summary>Boolean combination modes for <see cref="IGeometry.Combine"/>.</summary>
internal enum GeometryCombineMode
{
	Union,
	Intersect,
	Difference,
	Xor,
}
