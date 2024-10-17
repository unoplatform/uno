#nullable enable

using SkiaSharp;
using System;
using Windows.Graphics;

namespace Windows.UI.Composition
{
	internal class SkiaGeometrySource2D : IGeometrySource2D
	{
		public SkiaGeometrySource2D(SKPath source)
		{
			Geometry = source ?? throw new ArgumentNullException(nameof(source));
		}

		/// <remarks>
		/// DO NOT MODIFY THIS SKPath. CREATE A NEW SkiaGeometrySource2D INSTEAD.
		/// This can lead to nasty invalidation bugs where the SKPath changes without notifying anyone.
		/// </remarks>
		public SKPath Geometry { get; }
	}
}
