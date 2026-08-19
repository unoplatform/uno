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

		int IGeometry.SegmentCount => _geometry.PointCount;

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

		void IGeometry.StreamSegments(IGeometrySink sink)
		{
			using var it = _geometry.CreateIterator(false);
			var pts = new SKPoint[4];
			var inFigure = false;
			var closed = false;
			var current = default(SKPoint);
			SKPathVerb verb;
			while ((verb = it.Next(pts)) != SKPathVerb.Done)
			{
				switch (verb)
				{
					case SKPathVerb.Move:
						if (inFigure) { sink.EndFigure(closed); }
						sink.BeginFigure(new Vector2(pts[0].X, pts[0].Y));
						current = pts[0];
						inFigure = true;
						closed = false;
						break;
					case SKPathVerb.Line:
						sink.LineTo(new Vector2(pts[1].X, pts[1].Y));
						current = pts[1];
						break;
					case SKPathVerb.Quad:
						sink.QuadTo(new Vector2(pts[1].X, pts[1].Y), new Vector2(pts[2].X, pts[2].Y));
						current = pts[2];
						break;
					case SKPathVerb.Conic:
					{
						// No neutral conic segment — convert to (exact) quads and emit those.
						var quads = new SKPoint[5];
						var count = SKPath.ConvertConicToQuads(current, pts[1], pts[2], it.ConicWeight(), quads, 1);
						for (var i = 0; i < count; i++)
						{
							sink.QuadTo(new Vector2(quads[i * 2 + 1].X, quads[i * 2 + 1].Y), new Vector2(quads[i * 2 + 2].X, quads[i * 2 + 2].Y));
						}
						current = pts[2];
						break;
					}
					case SKPathVerb.Cubic:
						sink.CubicTo(new Vector2(pts[1].X, pts[1].Y), new Vector2(pts[2].X, pts[2].Y), new Vector2(pts[3].X, pts[3].Y));
						current = pts[3];
						break;
					case SKPathVerb.Close:
						closed = true;
						break;
				}
			}

			if (inFigure) { sink.EndFigure(closed); }
		}

		// Curve-flattening tolerance (path units ≈ device px at typical scales). Matches SkiaSharp's default curve
		// flatness so the stencil-fan tessellation density equals the reference's.
		private const float FlattenTolerance = 0.1f;

		// Adaptive segment count: scales with the curve's control-point deviation from its chord (the flattening
		// error bound), so tiny glyph curves cost ~2-4 segments and large curves get more. Clamped to [1, 24].
		private static int Steps(float deviation, float denom)
			=> Math.Clamp((int)MathF.Ceiling(MathF.Sqrt(deviation / (denom * FlattenTolerance))), 1, 24);

		private static float Dist(float ax, float ay, float bx, float by)
		{
			var dx = ax - bx; var dy = ay - by; return MathF.Sqrt(dx * dx + dy * dy);
		}

		private static void FlattenQuad(IFlattenedPathSink sink, SKPoint p0, SKPoint c, SKPoint p1)
		{
			var steps = Steps(Dist(c.X, c.Y, 0.5f * (p0.X + p1.X), 0.5f * (p0.Y + p1.Y)), 2f);
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
			var steps = Steps(Dist(c.X, c.Y, 0.5f * (p0.X + p1.X), 0.5f * (p0.Y + p1.Y)), 2f);
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
			var d1 = Dist(c1.X, c1.Y, p0.X + (p1.X - p0.X) / 3f, p0.Y + (p1.Y - p0.Y) / 3f);
			var d2 = Dist(c2.X, c2.Y, p0.X + 2f * (p1.X - p0.X) / 3f, p0.Y + 2f * (p1.Y - p0.Y) / 3f);
			var steps = Steps(MathF.Max(d1, d2), 0.75f);
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
