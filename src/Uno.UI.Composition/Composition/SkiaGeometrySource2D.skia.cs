#nullable enable

using SkiaSharp;
using System;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	internal class SkiaGeometrySource2D : CompositionObject, IGeometrySource2D
	{
		private SKPath _geometry;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public SkiaGeometrySource2D(SKPath source)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{
			Geometry = source ?? throw new ArgumentNullException(nameof(source));
		}

		/// <remarks>
		/// DO NOT MODIFY THIS SKPath. CREATE A NEW SKPath INSTEAD.
		/// This can lead to nasty invalidation bugs where the SKPath changes without notifying anyone.
		/// </remarks>
		public SKPath Geometry
		{
			get => _geometry;
			set => SetObjectProperty(ref _geometry, value);
		}
	}
}
