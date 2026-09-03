#nullable enable

using System;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Media
{
	partial class Geometry
	{
		// TODO: Can we mark Geometry and GetGeometry method as abstract?
		// While this will diverge from UWP, it doesn't seem to matter whether it's abstract or not because
		// this class doesn't have public constructors in UWP, which makes it not-inheritable either way.
		internal virtual IGeometry? GetGeometry() => throw new NotSupportedException($"Geometry {this} is not supported");

		/// <remarks>
		/// Note: Try not to depend on this. See the note in <see cref="Microsoft.UI.Composition.CompositionSpriteShape.NegativeFillGeometry"/>
		/// </remarks>
		internal virtual IGeometry? GetFilledGeometry() => null;

		/// <summary>
		/// Returns the geometry with the <see cref="Transform"/> applied, if any.
		/// </summary>
		internal IGeometry? GetTransformedGeometry() => ApplyTransform(GetGeometry());

		/// <summary>
		/// Returns the filled geometry with the <see cref="Transform"/> applied, if any.
		/// </summary>
		internal IGeometry? GetTransformedFilledGeometry() => ApplyTransform(GetFilledGeometry());

		private IGeometry? ApplyTransform(IGeometry? geometry)
		{
			if (geometry is null)
			{
				return null;
			}

			if (Transform is { MatrixCore: var matrix } && !matrix.IsIdentity)
			{
				return geometry.Transform(matrix);
			}

			return geometry;
		}
	}
}