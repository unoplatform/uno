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

		GeometryFillRule IGeometry.FillRule => _geometry.FillType == SKPathFillType.EvenOdd ? GeometryFillRule.EvenOdd : GeometryFillRule.NonZero;

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

		void IGeometry.StreamFlattened(IFlattenedPathSink sink)
		{
			using var it = _geometry.CreateIterator(false);
			var pts = new SKPoint[4];
			var inContour = false;
			var closed = false;
			var current = default(SKPoint);
			SKPathVerb verb;
			while ((verb = it.Next(pts)) != SKPathVerb.Done)
			{
				switch (verb)
				{
					case SKPathVerb.Move:
						if (inContour) { sink.EndContour(closed); }
						sink.BeginContour(new Vector2(pts[0].X, pts[0].Y));
						current = pts[0];
						inContour = true;
						closed = false;
						break;
					case SKPathVerb.Line:
						sink.LineTo(new Vector2(pts[1].X, pts[1].Y));
						current = pts[1];
						break;
					case SKPathVerb.Quad:
						FlattenQuad(sink, current, pts[1], pts[2]);
						current = pts[2];
						break;
					case SKPathVerb.Conic:
						FlattenConic(sink, current, pts[1], pts[2], it.ConicWeight());
						current = pts[2];
						break;
					case SKPathVerb.Cubic:
						FlattenCubic(sink, current, pts[1], pts[2], pts[3]);
						current = pts[3];
						break;
					case SKPathVerb.Close:
						closed = true;
						break;
				}
			}

			if (inContour) { sink.EndContour(closed); }
		}

		private static void FlattenQuad(IFlattenedPathSink sink, SKPoint p0, SKPoint c, SKPoint p1)
		{
			const int steps = 16;
			for (var i = 1; i <= steps; i++)
			{
				var t = i / (float)steps;
				var u = 1 - t;
				var x = u * u * p0.X + 2 * u * t * c.X + t * t * p1.X;
				var y = u * u * p0.Y + 2 * u * t * c.Y + t * t * p1.Y;
				sink.LineTo(new Vector2(x, y));
			}
		}

		private static void FlattenConic(IFlattenedPathSink sink, SKPoint p0, SKPoint c, SKPoint p1, float w)
		{
			const int steps = 16;
			for (var i = 1; i <= steps; i++)
			{
				var t = i / (float)steps;
				var u = 1 - t;
				var denom = u * u + 2 * u * t * w + t * t;
				var x = (u * u * p0.X + 2 * u * t * w * c.X + t * t * p1.X) / denom;
				var y = (u * u * p0.Y + 2 * u * t * w * c.Y + t * t * p1.Y) / denom;
				sink.LineTo(new Vector2(x, y));
			}
		}

		private static void FlattenCubic(IFlattenedPathSink sink, SKPoint p0, SKPoint c1, SKPoint c2, SKPoint p1)
		{
			const int steps = 24;
			for (var i = 1; i <= steps; i++)
			{
				var t = i / (float)steps;
				var u = 1 - t;
				var x = u * u * u * p0.X + 3 * u * u * t * c1.X + 3 * u * t * t * c2.X + t * t * t * p1.X;
				var y = u * u * u * p0.Y + 3 * u * u * t * c1.Y + 3 * u * t * t * c2.Y + t * t * t * p1.Y;
				sink.LineTo(new Vector2(x, y));
			}
		}

		public void Dispose() => _geometry.Dispose();
	}
}
