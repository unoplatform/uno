#nullable enable

using SkiaSharp;
using System;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	internal class SkiaGeometrySource2D : IGeometrySource2D, IDisposable
	{
		private readonly SKPath _geometry;

		public SkiaGeometrySource2D(SKPath source)
		{
			_geometry = source ?? throw new ArgumentNullException(nameof(source));
		}

		#region SKPath read-only passthrough methods

		public SkiaGeometrySource2D Transform(SKMatrix matrix)
		{
			var path = new SKPath();
			_geometry.Transform(matrix, path);
			return new SkiaGeometrySource2D(path);
		}

		public SKRect Bounds => _geometry.Bounds;
		public SKRect TightBounds => _geometry.TightBounds;

		public void CanvasDrawPath(SKCanvas canvas, SKPaint paint) => canvas.DrawPath(_geometry, paint);

		public void CanvasClipPath(SKCanvas canvas, SKClipOperation operation = SKClipOperation.Intersect, bool antialias = false) => canvas.ClipPath(_geometry, operation, antialias);

		public bool GetFillPath(SKPaint paint, SKPath dst) => paint.GetFillPath(_geometry, dst);

		public bool Contains(float x, float y) => _geometry.Contains(x, y);

		public SkiaGeometrySource2D Op(SkiaGeometrySource2D other, SKPathOp op) => new(_geometry.Op(other._geometry, op));

		/// <remarks>
		/// DO NOT MODIFY THIS SKPath. CREATE A NEW SkiaGeometrySource2D INSTEAD.
		/// This can lead to nasty invalidation bugs where the SKPath changes without notifying anyone.
		/// </remarks>
		public SKPath Geometry => _geometry;

		#endregion

		public void Dispose() => Geometry.Dispose();
	}
}
