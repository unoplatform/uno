#nullable enable

using SkiaSharp;
using System;
using Windows.Graphics;

namespace Windows.UI.Composition
{
	public class SkiaGeometrySource2D : IGeometrySource2D
	{
		public SkiaGeometrySource2D()
		{
			Geometry = new SKPath();
		}

		public SkiaGeometrySource2D(SKPath source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			Geometry = source;
		}

		public SKPath Geometry { get; }
	}
}
