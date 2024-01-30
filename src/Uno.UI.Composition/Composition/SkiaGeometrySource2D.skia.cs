#nullable enable

using SkiaSharp;
using System;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	public class SkiaGeometrySource2D : IGeometrySource2D, IDisposable
	{
		public SkiaGeometrySource2D()
		{
			Geometry = new SKPath();
		}

		public SkiaGeometrySource2D(SKPath source)
		{
			Geometry = source ?? throw new ArgumentNullException(nameof(source));
		}

		public SKPath Geometry { get; }

		public void Dispose() => Geometry.Dispose();
	}
}
