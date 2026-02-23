#nullable enable

using System;
using Windows.Foundation;
using SkiaSharp;
using Uno;
using Uno.Disposables;
using Uno.Extensions;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private static readonly SKPaint _spareHitTestPaint = new();
		private static readonly SKPath _spareHitTestPath = new();
		// We don't call SKPaint.Reset() after usage, so make sure
		// that only SKPaint.Color is being set
		private static readonly SKPaint _spareColorPaint = new();

		private CompositionGeometry? _fillGeometry;

		private SkiaGeometrySource2D? _geometryWithTransformations;
		private SkiaGeometrySource2D? _fillGeometryWithTransformations;

		/// <summary>
		/// This is largely a hack that's needed for MUX.Shapes.Path with Data set to a PathGeometry that has some
		/// figures with IsFilled = False. CompositionSpriteShapes don't have the concept of a "selectively filled
		/// geometry". The entire Geometry is either filled (FillBrush is not null) or not. To work around this,
		/// we add this "fill geometry" which is only the subgeomtry to be filled.
		/// cf. https://github.com/unoplatform/uno/issues/18694
		/// Remove this if we port Shapes from WinUI, which don't use CompositionSpriteShapes to begin with, but
		/// a CompositionMaskBrush that (presumably) masks out certain areas. We compensate for this by using this
		/// geometry as the mask.
		/// </summary>
		internal CompositionGeometry? FillGeometry
		{
			private get => _fillGeometry;
			set => SetProperty(ref _fillGeometry, value);
		}

		internal override bool CanPaint() => (FillBrush?.CanPaint() ?? false) || (StrokeBrush?.CanPaint() ?? false);

		private static readonly SKPaint _sparePaint = new SKPaint();
		private static readonly SKPath _sparePath = new SKPath();

		internal override void Paint(in Visual.PaintingSession session)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				if (FillBrush is { } fill && _fillGeometryWithTransformations is { } finalFillGeometryWithTransformations)
				{
					var fillPaint = _sparePaint;
					PrepareTempPaint(fillPaint, isStroke: false);

					if (Geometry is not null && (Geometry.TrimStart != default || Geometry.TrimEnd != default))
					{
						fillPaint.PathEffect = SKPathEffect.CreateTrim(Geometry.TrimStart, Geometry.TrimEnd);
					}

					var fillPath = _sparePath;
					fillPath.Rewind();
					finalFillGeometryWithTransformations.GetFillPath(fillPaint, fillPath);

					session.Canvas.Save();
					session.Canvas.ClipPath(fillPath, antialias: true);
					if (Compositor.TryGetEffectiveBackgroundColor(this, out var colorFromTransition))
					{
						_spareColorPaint.Color = colorFromTransition.ToSKColor(session.Opacity);
						session.Canvas.DrawRect(fillPath.Bounds, _spareColorPaint);
					}
					else
					{
						fill.Paint(session.Canvas, session.Opacity, finalFillGeometryWithTransformations.Bounds);
					}
					session.Canvas.Restore();
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					var strokePaint = _sparePaint;
					PrepareTempPaint(strokePaint, isStroke: true);

					// Set stroke thickness
					strokePaint.StrokeWidth = StrokeThickness;

					// Set stroke join and miter limit
					strokePaint.StrokeJoin = ToSKStrokeJoin(StrokeLineJoin);
					strokePaint.StrokeMiter = StrokeMiterLimit;

					// Determine if we need custom cap rendering (different start/end caps, or Triangle caps)
					bool needsCustomCaps = StrokeStartCap != StrokeEndCap
						|| StrokeStartCap == CompositionStrokeCap.Triangle;

					float[]? dashValues = null;
					if (StrokeDashArray is { Count: > 0 } strokeDashArray)
					{
						strokePaint.StrokeCap = ToSKStrokeCap(StrokeDashCap);
						// WinUI dash values are in multiples of StrokeThickness; Skia expects pixels
						dashValues = strokeDashArray.ToEvenArray();
						for (int i = 0; i < dashValues.Length; i++)
						{
							dashValues[i] *= StrokeThickness;
						}
						strokePaint.PathEffect = SKPathEffect.CreateDash(dashValues, StrokeDashOffset * StrokeThickness);
					}
					else if (!needsCustomCaps)
					{
						// Fast path: same start/end cap, not Triangle - use native SKPaint.StrokeCap
						strokePaint.StrokeCap = ToSKStrokeCap(StrokeEndCap);
					}
					// else: needsCustomCaps without dashes - keep Butt cap (default after Reset),
					// custom caps are added as geometry below.

					if (Geometry is not null && (Geometry.TrimStart != default || Geometry.TrimEnd != default))
					{
						var pathEffect = SKPathEffect.CreateTrim(Geometry.TrimStart, Geometry.TrimEnd);
						if (strokePaint.PathEffect is SKPathEffect effect)
						{
							pathEffect = SKPathEffect.CreateSum(effect, pathEffect);
						}

						strokePaint.PathEffect = pathEffect;
					}

					// Generate stroke geometry for bounds that will be passed to a brush.
					// - [Future]: This generated geometry should also be used for hit testing.

					// If we have something like this:
					// <Path Data="M 0 0 L 50 0 L 50 50 L 0 50 z"
					//		 Stroke="Red"
					//		 StrokeThickness="5"
					//		 Width="70"
					//		 Stretch="Fill"
					//		 HorizontalAlignment="Center"
					//		 VerticalAlignment="Center" />
					// The geometry itself is a 50x50 rectangle, and then we set the shape Width to 70 and let it
					// to stretch over the available height, and we have a stroke thickness as 1px
					// On Windows, the stroke is simply 1px, it doesn't scale with the height.
					// So, to get a correct stroke geometry, we must apply the transformations first.

					var strokeFillPath = _sparePath;
					strokeFillPath.Rewind();
					// Get the stroke geometry, after scaling has been applied.
					geometryWithTransformations.GetFillPath(strokePaint, strokeFillPath);

					// Add custom cap geometry for Triangle caps or different start/end caps
					if (needsCustomCaps && StrokeDashArray is not { Count: > 0 })
					{
						AddCustomCaps(strokeFillPath, geometryWithTransformations.Geometry, StrokeThickness, StrokeStartCap, StrokeEndCap);
					}

					// Fix endpoint caps for dashed strokes: WinUI uses StartCap/EndCap at path
					// endpoints but DashCap at internal dash boundaries. Skia applies DashCap uniformly.
					if (dashValues is not null
						&& (StrokeDashCap != StrokeStartCap || StrokeDashCap != StrokeEndCap))
					{
						FixDashEndpointCaps(strokeFillPath, geometryWithTransformations.Geometry,
							StrokeThickness, StrokeDashCap, StrokeStartCap, StrokeEndCap,
							dashValues, StrokeDashOffset * StrokeThickness);
					}

					session.Canvas.Save();
					session.Canvas.ClipPath(strokeFillPath, antialias: true);
					stroke.Paint(session.Canvas, session.Opacity, strokeFillPath.Bounds);
					session.Canvas.Restore();
				}
			}
		}

		private static void PrepareTempPaint(SKPaint paint, bool isStroke)
		{
			paint.Reset();
			paint.IsAntialias = true;
			paint.IsStroke = isStroke;
			paint.Color = SKColors.White;   // Transparent color wouldn't draw anything
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Geometry) or nameof(CombinedTransformMatrix) or nameof(FillGeometry):
					if (Geometry?.BuildGeometry() is SkiaGeometrySource2D geometry)
					{
						var transform = CombinedTransformMatrix;
						_geometryWithTransformations = transform.IsIdentity
							? geometry
							: geometry.Transform(transform.ToSKMatrix());
						if (FillGeometry?.BuildGeometry() is SkiaGeometrySource2D fillGeometry)
						{
							_fillGeometryWithTransformations = transform.IsIdentity
								? fillGeometry
								: fillGeometry.Transform(transform.ToSKMatrix());
						}
						else
						{
							_fillGeometryWithTransformations = _geometryWithTransformations;
						}
					}
					else
					{
						_geometryWithTransformations = null;
						_fillGeometryWithTransformations = null;
					}
					break;
			}
		}

		internal override bool HitTest(Point point)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				point = CombinedTransformMatrix.Inverse().Transform(point);

				if (FillBrush is { } && geometryWithTransformations.Contains((float)point.X, (float)point.Y))
				{
					return true;
				}

				if (StrokeBrush is { } && StrokeThickness > 0)
				{
					var strokePaint = _spareHitTestPaint;
					PrepareTempPaint(strokePaint, isStroke: true);

					strokePaint.StrokeWidth = StrokeThickness;
					strokePaint.StrokeJoin = ToSKStrokeJoin(StrokeLineJoin);
					strokePaint.StrokeMiter = StrokeMiterLimit;

					bool needsCustomCaps = StrokeStartCap != StrokeEndCap
						|| StrokeStartCap == CompositionStrokeCap.Triangle;

					float[]? dashValues = null;
					if (StrokeDashArray is { Count: > 0 } strokeDashArray)
					{
						strokePaint.StrokeCap = ToSKStrokeCap(StrokeDashCap);
						// WinUI dash values are in multiples of StrokeThickness; Skia expects pixels
						dashValues = strokeDashArray.ToEvenArray();
						for (int i = 0; i < dashValues.Length; i++)
						{
							dashValues[i] *= StrokeThickness;
						}
						strokePaint.PathEffect = SKPathEffect.CreateDash(dashValues, StrokeDashOffset * StrokeThickness);
					}
					else if (!needsCustomCaps)
					{
						strokePaint.StrokeCap = ToSKStrokeCap(StrokeEndCap);
					}

					var hitTestStrokeFillPath = _spareHitTestPath;

					hitTestStrokeFillPath.Rewind();

					geometryWithTransformations.GetFillPath(strokePaint, hitTestStrokeFillPath);

					if (needsCustomCaps && StrokeDashArray is not { Count: > 0 })
					{
						AddCustomCaps(hitTestStrokeFillPath, geometryWithTransformations.Geometry, StrokeThickness, StrokeStartCap, StrokeEndCap);
					}

					// Fix endpoint caps for dashed strokes (mirror of Paint logic)
					if (dashValues is not null
						&& (StrokeDashCap != StrokeStartCap || StrokeDashCap != StrokeEndCap))
					{
						FixDashEndpointCaps(hitTestStrokeFillPath, geometryWithTransformations.Geometry,
							StrokeThickness, StrokeDashCap, StrokeStartCap, StrokeEndCap,
							dashValues, StrokeDashOffset * StrokeThickness);
					}

					if (hitTestStrokeFillPath.Contains((float)point.X, (float)point.Y))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static SKStrokeCap ToSKStrokeCap(CompositionStrokeCap cap) => cap switch
		{
			CompositionStrokeCap.Flat => SKStrokeCap.Butt,
			CompositionStrokeCap.Square => SKStrokeCap.Square,
			CompositionStrokeCap.Round => SKStrokeCap.Round,
			CompositionStrokeCap.Triangle => SKStrokeCap.Butt, // Simulated via custom geometry
			_ => SKStrokeCap.Butt,
		};

		private static SKStrokeJoin ToSKStrokeJoin(CompositionStrokeLineJoin join) => join switch
		{
			CompositionStrokeLineJoin.Miter => SKStrokeJoin.Miter,
			CompositionStrokeLineJoin.Bevel => SKStrokeJoin.Bevel,
			CompositionStrokeLineJoin.Round => SKStrokeJoin.Round,
			CompositionStrokeLineJoin.MiterOrBevel => SKStrokeJoin.Miter, // Skia's miter limit provides bevel fallback
			_ => SKStrokeJoin.Miter,
		};

		/// <summary>
		/// Adds custom cap geometry to the stroke fill path for cases where native SKPaint.StrokeCap
		/// is insufficient (different start/end caps, or Triangle cap type).
		/// </summary>
		private static void AddCustomCaps(SKPath fillPath, SKPath originalGeometry, float strokeWidth, CompositionStrokeCap startCap, CompositionStrokeCap endCap)
		{
			using var measure = new SKPathMeasure(originalGeometry, false);
			do
			{
				if (measure.IsClosed)
				{
					continue;
				}

				var length = measure.Length;
				if (length <= 0)
				{
					continue;
				}

				// Start cap: tangent direction is negated (cap extends backward from start)
				if (startCap != CompositionStrokeCap.Flat
					&& measure.GetPositionAndTangent(0, out var startPos, out var startTan))
				{
					using var capPath = BuildCapPath(startPos, new SKPoint(-startTan.X, -startTan.Y), strokeWidth, startCap);
					if (capPath != null)
					{
						fillPath.AddPath(capPath);
					}
				}

				// End cap: tangent direction as-is (cap extends forward from end)
				if (endCap != CompositionStrokeCap.Flat
					&& measure.GetPositionAndTangent(length, out var endPos, out var endTan))
				{
					using var capPath = BuildCapPath(endPos, endTan, strokeWidth, endCap);
					if (capPath != null)
					{
						fillPath.AddPath(capPath);
					}
				}
			} while (measure.NextContour());
		}

		/// <summary>
		/// Fixes endpoint caps on dashed strokes. WinUI applies StrokeStartCap at path start and
		/// StrokeEndCap at path end, but StrokeDashCap at internal dash boundaries. Skia's
		/// GetFillPath with a dash effect applies DashCap uniformly. This method corrects the
		/// endpoints by removing the incorrect DashCap protrusion and adding the correct cap.
		/// </summary>
		private static void FixDashEndpointCaps(
			SKPath fillPath,
			SKPath originalGeometry,
			float strokeWidth,
			CompositionStrokeCap dashCap,
			CompositionStrokeCap startCap,
			CompositionStrokeCap endCap,
			float[] dashValues,
			float dashOffset)
		{
			using var measure = new SKPathMeasure(originalGeometry, false);
			do
			{
				if (measure.IsClosed)
				{
					continue;
				}

				var length = measure.Length;
				if (length <= 0)
				{
					continue;
				}

				// Fix start cap
				if (dashCap != startCap
					&& measure.GetPositionAndTangent(0, out var startPos, out var startTan)
					&& IsPositionInDash(0, dashValues, dashOffset))
				{
					var backDir = new SKPoint(-startTan.X, -startTan.Y);

					// Remove the incorrect DashCap protrusion at start
					if (dashCap != CompositionStrokeCap.Flat)
					{
						using var cutter = BuildHalfPlaneCutter(startPos, backDir, strokeWidth);
						using var result = new SKPath();
						if (fillPath.Op(cutter, SKPathOp.Difference, result))
						{
							fillPath.Rewind();
							fillPath.AddPath(result);
						}
					}

					// Add the correct StartCap
					if (startCap != CompositionStrokeCap.Flat)
					{
						using var capPath = BuildCapPath(startPos, backDir, strokeWidth, startCap);
						if (capPath != null)
						{
							fillPath.AddPath(capPath);
						}
					}
				}

				// Fix end cap
				if (dashCap != endCap
					&& measure.GetPositionAndTangent(length, out var endPos, out var endTan)
					&& IsPositionInDash(length, dashValues, dashOffset))
				{
					// Remove the incorrect DashCap protrusion at end
					if (dashCap != CompositionStrokeCap.Flat)
					{
						using var cutter = BuildHalfPlaneCutter(endPos, endTan, strokeWidth);
						using var result = new SKPath();
						if (fillPath.Op(cutter, SKPathOp.Difference, result))
						{
							fillPath.Rewind();
							fillPath.AddPath(result);
						}
					}

					// Add the correct EndCap
					if (endCap != CompositionStrokeCap.Flat)
					{
						using var capPath = BuildCapPath(endPos, endTan, strokeWidth, endCap);
						if (capPath != null)
						{
							fillPath.AddPath(capPath);
						}
					}
				}
			} while (measure.NextContour());
		}

		/// <summary>
		/// Builds a half-plane rectangle extending from an endpoint in the cap direction.
		/// Used to cut away incorrect DashCap protrusions at path endpoints.
		/// </summary>
		private static SKPath BuildHalfPlaneCutter(SKPoint position, SKPoint direction, float strokeWidth)
		{
			var size = strokeWidth * 2;
			var normal = new SKPoint(-direction.Y, direction.X);

			var p1 = new SKPoint(position.X + normal.X * size, position.Y + normal.Y * size);
			var p2 = new SKPoint(p1.X + direction.X * size, p1.Y + direction.Y * size);
			var p3 = new SKPoint(position.X - normal.X * size + direction.X * size, position.Y - normal.Y * size + direction.Y * size);
			var p4 = new SKPoint(position.X - normal.X * size, position.Y - normal.Y * size);

			var path = new SKPath();
			path.AddPoly(new[] { p1, p2, p3, p4 }, close: true);
			return path;
		}

		/// <summary>
		/// Determines whether a given position along a path falls within a dash (true) or a gap (false)
		/// in the dash pattern.
		/// </summary>
		private static bool IsPositionInDash(float position, float[] dashValues, float dashOffset)
		{
			// Compute total dash pattern length
			var totalLength = 0f;
			for (int i = 0; i < dashValues.Length; i++)
			{
				totalLength += dashValues[i];
			}

			if (totalLength <= 0)
			{
				return true;
			}

			// Normalize position within pattern, accounting for offset
			var patternPos = (position + dashOffset) % totalLength;
			if (patternPos < 0)
			{
				patternPos += totalLength;
			}

			// Walk through dash values to find which segment the position falls in.
			// Even indices are dashes, odd indices are gaps.
			var accumulated = 0f;
			for (int i = 0; i < dashValues.Length; i++)
			{
				accumulated += dashValues[i];
				if (patternPos < accumulated)
				{
					return i % 2 == 0; // Even = dash, odd = gap
				}
			}

			// Edge case: position exactly at pattern boundary - treat as start of dash
			return true;
		}

		/// <summary>
		/// Builds a cap shape at the given position extending in the given direction.
		/// </summary>
		private static SKPath? BuildCapPath(SKPoint position, SKPoint direction, float strokeWidth, CompositionStrokeCap capType)
		{
			var halfWidth = strokeWidth / 2;
			// Normal perpendicular to direction
			var normal = new SKPoint(-direction.Y, direction.X);

			if (capType == CompositionStrokeCap.Round)
			{
				var path = new SKPath();
				// Build a semicircle oriented in the cap direction
				var startAngle = (float)(Math.Atan2(normal.Y, normal.X) * 180 / Math.PI);
				var rect = new SKRect(
					position.X - halfWidth,
					position.Y - halfWidth,
					position.X + halfWidth,
					position.Y + halfWidth);
				path.AddArc(rect, startAngle, -180);
				path.Close();
				return path;
			}
			else if (capType == CompositionStrokeCap.Square)
			{
				var path = new SKPath();
				// Rectangle extending halfWidth beyond endpoint in direction
				var p1 = new SKPoint(position.X + normal.X * halfWidth, position.Y + normal.Y * halfWidth);
				var p2 = new SKPoint(p1.X + direction.X * halfWidth, p1.Y + direction.Y * halfWidth);
				var p3 = new SKPoint(p2.X - normal.X * strokeWidth, p2.Y - normal.Y * strokeWidth);
				var p4 = new SKPoint(position.X - normal.X * halfWidth, position.Y - normal.Y * halfWidth);
				path.AddPoly(new[] { p1, p2, p3, p4 }, close: true);
				return path;
			}
			else if (capType == CompositionStrokeCap.Triangle)
			{
				var path = new SKPath();
				// Isoceles triangle: base perpendicular to direction at endpoint, apex at halfWidth in direction
				var base1 = new SKPoint(position.X + normal.X * halfWidth, position.Y + normal.Y * halfWidth);
				var apex = new SKPoint(position.X + direction.X * halfWidth, position.Y + direction.Y * halfWidth);
				var base2 = new SKPoint(position.X - normal.X * halfWidth, position.Y - normal.Y * halfWidth);
				path.AddPoly(new[] { base1, apex, base2 }, close: true);
				return path;
			}

			return null;
		}
	}
}
