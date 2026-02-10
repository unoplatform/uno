using System;
using SkiaSharp;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Media
{
	partial class Geometry
	{
		// TODO: Can we mark Geometry and GetSKPath method as abstract?
		// While this will diverge from UWP, it doesn't seem to matter whether it's abstract or not because
		// this class doesn't have public constructors in UWP, which makes it not-inheritable either way.
		internal virtual SKPath GetSKPath() => throw new NotSupportedException($"Geometry {this} is not supported");

		/// <remarks>
		/// Note: Try not to depend on this. See the note in <see cref="CompositionSpriteShape.NegativeFillGeometry"/>
		/// </remarks>
		internal virtual SKPath GetFilledSKPath() => null;

		/// <summary>
		/// Returns the SKPath with the <see cref="Transform"/> applied, if any.
		/// </summary>
		internal SKPath GetTransformedSKPath()
		{
			var path = GetSKPath();
			return ApplyTransformToPath(path);
		}

		/// <summary>
		/// Returns the filled SKPath with the <see cref="Transform"/> applied, if any.
		/// </summary>
		internal SKPath GetTransformedFilledSKPath()
		{
			var path = GetFilledSKPath();
			if (path is null)
			{
				return null;
			}

			return ApplyTransformToPath(path);
		}

		private SKPath ApplyTransformToPath(SKPath path)
		{
			if (Transform is { MatrixCore: var matrix } && !matrix.IsIdentity)
			{
				var skMatrix = matrix.ToSKMatrix();
				var transformed = new SKPath(path);
				transformed.Transform(skMatrix);
				return transformed;
			}

			return path;
		}

		internal virtual SkiaGeometrySource2D GetGeometrySource2D() => new SkiaGeometrySource2D(new SKPath(GetTransformedSKPath()));
	}
}
