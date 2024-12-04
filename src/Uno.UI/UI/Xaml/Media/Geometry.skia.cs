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

		internal virtual SkiaGeometrySource2D GetGeometrySource2D() => new SkiaGeometrySource2D(new SKPath(GetSKPath()));
	}
}
