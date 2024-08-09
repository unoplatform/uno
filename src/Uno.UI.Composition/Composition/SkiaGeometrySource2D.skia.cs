#nullable enable

using SkiaSharp;
using System;
using Windows.Graphics;

namespace Windows.UI.Composition
{
	internal class SkiaGeometrySource2D : IGeometrySource2D
	{
		public SkiaGeometrySource2D()
		{
			Geometry = new SKPath();
		}

		public SkiaGeometrySource2D(SKPath source)
		{
			Geometry = source ?? throw new ArgumentNullException(nameof(source));
		}

		public SKPath Geometry { get; set; }
	}
}
