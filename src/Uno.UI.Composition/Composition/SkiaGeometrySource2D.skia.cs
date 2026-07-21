#nullable enable

using System;
using System.Numerics;
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Windows.Foundation;
using Windows.Graphics;

namespace Microsoft.UI.Composition
{
	internal partial class SkiaGeometrySource2D : IGeometrySource2D, IGeometry, IDisposable
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

		public void CanvasDrawPath(SKCanvas canvas, SKPaint paint) => canvas.DrawPath(_geometry, paint);

		public void CanvasClipPath(SKCanvas canvas, SKClipOperation operation = SKClipOperation.Intersect, bool antialias = false) => canvas.ClipPath(_geometry, operation, antialias);

		public bool GetFillPath(SKPaint paint, SKPathBuilder dst) => paint.GetFillPath(_geometry, dst);

		public bool Contains(float x, float y) => _geometry.Contains(x, y);

		public SkiaGeometrySource2D Op(SkiaGeometrySource2D other, SKPathOp op) => new(_geometry.Op(other._geometry, op));

		#endregion

		/// <remarks>
		/// DO NOT MODIFY THIS SKPath. CREATE A NEW SkiaGeometrySource2D INSTEAD.
		/// This can lead to nasty invalidation bugs where the SKPath changes without notifying anyone.
		/// </remarks>
		public SKPath Geometry => _geometry;

		#region IGeometry (backend-neutral handle)

		Rect IGeometry.Bounds => _geometry.TightBounds.ToRect();

		bool IGeometry.IsEmpty => _geometry.IsEmpty;

		bool IGeometry.FillContains(Vector2 point) => _geometry.Contains(point.X, point.Y);

		IGeometry IGeometry.Transform(Matrix3x2 matrix) => Transform(matrix.ToSKMatrix());

		IGeometry IGeometry.Combine(IGeometry other, GeometryCombineMode mode)
		{
			using var lease = SkiaGeometryInterop.Lease(other);
			return new SkiaGeometrySource2D(_geometry.Op(lease.Path, mode switch
			{
				GeometryCombineMode.Union => SKPathOp.Union,
				GeometryCombineMode.Intersect => SKPathOp.Intersect,
				GeometryCombineMode.Difference => SKPathOp.Difference,
				GeometryCombineMode.Xor => SKPathOp.Xor,
				_ => SKPathOp.Union,
			}));
		}

		#endregion

		public void Dispose() => _geometry.Dispose();
	}
}
