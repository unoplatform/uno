using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Graphics;

namespace Windows.UI.Composition
{
	public class SkiaGeometrySource2D : IGeometrySource2D
	{
		public SkiaGeometrySource2D(SKPath source = null)
		{
			Geometry = new SKPath();
		}

		public SKPath Geometry { get; }
	}
}
