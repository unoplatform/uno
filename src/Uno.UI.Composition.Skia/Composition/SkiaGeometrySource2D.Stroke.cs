#nullable enable

using System;
using SkiaSharp;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition
{
	internal partial class SkiaGeometrySource2D
	{
		// Per-thread scratch: stroke/fill geometry is built on both the record (UI/dispatcher) thread and the present
		// (render) thread, so these must NOT be shared process-wide — concurrent Reset/GetFillPath/Detach on one
		// SKPathBuilder corrupts the in-flight build and the resulting SKPath faults a later SKPath.Op. [ThreadStatic]
		// gives each thread its own spare (matching SkiaDrawingSession._sparePaint). Lazily created per thread since
		// [ThreadStatic] can't carry an initializer beyond the first thread.
		[ThreadStatic]
		private static SKPaint? _strokeSparePaintTls;
		private static SKPaint _strokeSparePaint => _strokeSparePaintTls ??= new();

		[ThreadStatic]
		private static SKPathBuilder? _strokeSpareBuilderTls;
		private static SKPathBuilder _strokeSpareBuilder => _strokeSpareBuilderTls ??= new();

		[ThreadStatic]
		private static SKPoint[]? _spareMiterPointsTls;
		private static SKPoint[] _spareMiterPoints => _spareMiterPointsTls ??= new SKPoint[4];

		IGeometry IGeometry.GetFilledGeometry(float trimStart, float trimEnd)
		{
			var paint = _strokeSparePaint;
			paint.Reset();
			paint.IsAntialias = true;
			paint.IsStroke = false;
			paint.Color = SKColors.White;
			if (trimStart != default || trimEnd != default)
			{
				paint.PathEffect = SKPathEffect.CreateTrim(trimStart, trimEnd);
			}

			var builder = _strokeSpareBuilder;
			builder.Reset();
			paint.GetFillPath(_geometry, builder);
			return new SkiaGeometrySource2D(builder.Detach());
		}

		IGeometry IGeometry.GetStrokeFillGeometry(in StrokeStyle style)
		{
			var paint = _strokeSparePaint;
			paint.Reset();
			paint.IsAntialias = true;
			paint.IsStroke = true;
			paint.Color = SKColors.White;
			paint.StrokeWidth = style.Thickness;
			paint.StrokeJoin = ToSKStrokeJoin(style.LineJoin);
			paint.StrokeMiter = style.MiterLimit;

			var needsCustomCaps = style.StartCap != style.EndCap || style.StartCap == StrokeCap.Triangle;

			float[]? dashValues = null;
			if (style.DashArray is { Length: > 0 } dashArray)
			{
				paint.StrokeCap = ToSKStrokeCap(style.DashCap);
				dashValues = new float[dashArray.Length];
				for (var i = 0; i < dashArray.Length; i++)
				{
					dashValues[i] = dashArray[i] * style.Thickness;
				}

				var dashEffect = SKPathEffect.CreateDash(dashValues, style.DashOffset * style.Thickness);
				if (dashEffect is not null)
				{
					paint.PathEffect = dashEffect;
				}
				else
				{
					dashValues = null;
				}
			}
			else if (!needsCustomCaps)
			{
				paint.StrokeCap = ToSKStrokeCap(style.EndCap);
			}

			if (style.TrimStart != default || style.TrimEnd != default)
			{
				var trim = SKPathEffect.CreateTrim(style.TrimStart, style.TrimEnd);
				paint.PathEffect = paint.PathEffect is { } existing ? SKPathEffect.CreateSum(existing, trim) : trim;
			}

			var builder = _strokeSpareBuilder;
			builder.Reset();
			paint.GetFillPath(_geometry, builder);

			if (needsCustomCaps && style.DashArray is not { Length: > 0 })
			{
				AddCustomCaps(builder, _geometry, style.Thickness, style.StartCap, style.EndCap);
			}

			if (dashValues is not null
				&& (style.DashCap != StrokeCap.Butt
					|| style.StartCap != StrokeCap.Butt
					|| style.EndCap != StrokeCap.Butt))
			{
				FixDashEndpointCaps(builder, _geometry, style.Thickness, style.DashCap, style.StartCap, style.EndCap,
					dashValues, style.DashOffset * style.Thickness);
			}

			if (dashValues is not null && style.DashCap == StrokeCap.Triangle)
			{
				AddInternalTriangleDashCaps(builder, _geometry, style.Thickness, dashValues, style.DashOffset * style.Thickness);
			}

			if (style.LineJoin == StrokeJoin.Miter)
			{
				AddClippedMiterJoints(builder, _geometry, style.Thickness, style.MiterLimit);
			}

			return new SkiaGeometrySource2D(builder.Detach());
		}

		private static SKStrokeCap ToSKStrokeCap(StrokeCap cap) => cap switch
		{
			StrokeCap.Butt => SKStrokeCap.Butt,
			StrokeCap.Square => SKStrokeCap.Square,
			StrokeCap.Round => SKStrokeCap.Round,
			StrokeCap.Triangle => SKStrokeCap.Butt, // Simulated via custom geometry
			_ => SKStrokeCap.Butt,
		};

		private static SKStrokeJoin ToSKStrokeJoin(StrokeJoin join) => join switch
		{
			StrokeJoin.Miter => SKStrokeJoin.Miter,
			StrokeJoin.Bevel => SKStrokeJoin.Bevel,
			StrokeJoin.Round => SKStrokeJoin.Round,
			StrokeJoin.MiterOrBevel => SKStrokeJoin.Miter, // Skia's miter limit provides bevel fallback
			_ => SKStrokeJoin.Miter,
		};

		/// <summary>
		/// Adds custom cap geometry to the stroke fill path for cases where native SKPaint.StrokeCap
		/// is insufficient (different start/end caps, or Triangle cap type).
		/// </summary>
		private static void AddCustomCaps(SKPathBuilder fillPath, SKPath originalGeometry, float strokeWidth, StrokeCap startCap, StrokeCap endCap)
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
				if (startCap != StrokeCap.Butt
					&& measure.GetPositionAndTangent(0, out var startPos, out var startTan))
				{
					using var capPath = BuildCapPath(startPos, new SKPoint(-startTan.X, -startTan.Y), strokeWidth, startCap);
					if (capPath != null)
					{
						fillPath.AddPath(capPath, SKPathAddMode.Append);
					}
				}

				// End cap: tangent direction as-is (cap extends forward from end)
				if (endCap != StrokeCap.Butt
					&& measure.GetPositionAndTangent(length, out var endPos, out var endTan))
				{
					using var capPath = BuildCapPath(endPos, endTan, strokeWidth, endCap);
					if (capPath != null)
					{
						fillPath.AddPath(capPath, SKPathAddMode.Append);
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
			SKPathBuilder fillPath,
			SKPath originalGeometry,
			float strokeWidth,
			StrokeCap dashCap,
			StrokeCap startCap,
			StrokeCap endCap,
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
					if (dashCap != StrokeCap.Butt)
					{
						using var cutter = BuildHalfPlaneCutter(startPos, backDir, strokeWidth);
						using var current = fillPath.Snapshot();
						using var result = new SKPath();
						if (current.Op(cutter, SKPathOp.Difference, result))
						{
							fillPath.Reset();
							fillPath.AddPath(result, SKPathAddMode.Append);
						}
					}

					// Add the correct StartCap
					if (startCap != StrokeCap.Butt)
					{
						using var capPath = BuildCapPath(startPos, backDir, strokeWidth, startCap);
						if (capPath != null)
						{
							fillPath.AddPath(capPath, SKPathAddMode.Append);
						}
					}
				}

				// Fix end cap
				if (measure.GetPositionAndTangent(length, out var endPos, out var endTan))
				{
					var endState = GetEndpointDashState(length, dashValues, dashOffset);

					if (endState == EndpointDashState.InRenderedDash)
					{
						// Rendered dash at endpoint → swap DashCap → EndCap if they differ.
						if (dashCap != endCap)
						{
							if (dashCap != StrokeCap.Butt)
							{
								using var cutter = BuildHalfPlaneCutter(endPos, endTan, strokeWidth);
								using var current = fillPath.Snapshot();
								using var result = new SKPath();
								if (current.Op(cutter, SKPathOp.Difference, result))
								{
									fillPath.Reset();
									fillPath.AddPath(result, SKPathAddMode.Append);
								}
							}
							if (endCap != StrokeCap.Butt)
							{
								using var capPath = BuildCapPath(endPos, endTan, strokeWidth, endCap);
								if (capPath != null)
								{
									fillPath.AddPath(capPath, SKPathAddMode.Append);
								}
							}
						}
					}
					else if (endState == EndpointDashState.AtGapBoundary)
					{
						// Endpoint at gap/dash boundary → zero-length dash.
						// DashCap facing backward + EndCap facing forward.
						var backDir = new SKPoint(-endTan.X, -endTan.Y);
						if (dashCap != StrokeCap.Butt)
						{
							using var capPath = BuildCapPath(endPos, backDir, strokeWidth, dashCap);
							if (capPath != null)
							{
								fillPath.AddPath(capPath, SKPathAddMode.Append);
							}
						}
						if (endCap != StrokeCap.Butt)
						{
							using var capPath = BuildCapPath(endPos, endTan, strokeWidth, endCap);
							if (capPath != null)
							{
								fillPath.AddPath(capPath, SKPathAddMode.Append);
							}
						}
					}
					// else: InGap — endpoint in middle of a gap, nothing to render
				}
			} while (measure.NextContour());
		}

		/// <summary>
		/// Adds Triangle cap geometry at every internal dash boundary. Skia maps Triangle
		/// to Butt (flat), so internal dash starts/ends need filled polygon caps added manually.
		/// Path start/end boundaries are excluded for open contours (handled by FixDashEndpointCaps).
		/// </summary>
		private static void AddInternalTriangleDashCaps(
			SKPathBuilder fillPath,
			SKPath originalGeometry,
			float strokeWidth,
			float[] dashValues,
			float dashOffset)
		{
			var totalPattern = 0f;
			for (int i = 0; i < dashValues.Length; i++)
			{
				totalPattern += dashValues[i];
			}

			if (totalPattern <= 0)
			{
				return;
			}

			using var measure = new SKPathMeasure(originalGeometry, false);
			do
			{
				var pathLength = measure.Length;
				if (pathLength <= 0)
				{
					continue;
				}

				var isClosed = measure.IsClosed;

				var patternStart = -(dashOffset % totalPattern);
				if (patternStart > 0)
				{
					patternStart -= totalPattern;
				}

				var pos = patternStart;
				var idx = 0;

				while (pos < pathLength)
				{
					var segLen = dashValues[idx % dashValues.Length];

					if (segLen <= 0)
					{
						idx++;
						continue;
					}

					var segEnd = pos + segLen;
					var isDash = (idx % 2) == 0;

					if (isDash)
					{
						var dashStart = Math.Max(pos, 0f);
						var dashEnd = Math.Min(segEnd, pathLength);

						if (dashStart < dashEnd)
						{
							// Dash start boundary: triangle faces backward (-tangent).
							// Skip for open path start (StrokeStartLineCap covers it).
							var isPathStart = !isClosed && dashStart <= 0f;
							if (!isPathStart
								&& measure.GetPositionAndTangent(dashStart, out var startPos, out var startTan))
							{
								var backDir = new SKPoint(-startTan.X, -startTan.Y);
								using var capPath = BuildCapPath(startPos, backDir, strokeWidth, StrokeCap.Triangle);
								if (capPath != null)
								{
									fillPath.AddPath(capPath, SKPathAddMode.Append);
								}
							}

							// Dash end boundary: triangle faces forward (+tangent).
							// Skip for open path end (StrokeEndLineCap covers it).
							var isPathEnd = !isClosed && dashEnd >= pathLength;
							if (!isPathEnd
								&& measure.GetPositionAndTangent(dashEnd, out var endPos, out var endTan))
							{
								using var capPath = BuildCapPath(endPos, endTan, strokeWidth, StrokeCap.Triangle);
								if (capPath != null)
								{
									fillPath.AddPath(capPath, SKPathAddMode.Append);
								}
							}
						}
					}

					pos = segEnd;
					idx++;
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

			var builder = new SKPathBuilder();
			builder.AddPoly(new[] { p1, p2, p3, p4 }, true);
			return builder.Detach();
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

		private enum EndpointDashState
		{
			InRenderedDash, // Endpoint within a dash Skia rendered (nonzero length)
			AtGapBoundary,  // Endpoint at/near end of a gap (within tolerance)
			InGap           // Endpoint in middle of a gap (no action needed)
		}

		/// <summary>
		/// Determines the dash state at the path endpoint by walking the pattern cumulatively.
		/// WinUI only creates a zero-length dash when the endpoint coincides with a gap/dash
		/// boundary (within MIN_DASH_ARRAY_LENGTH = 0.1px tolerance). When the endpoint is
		/// in the middle of a gap, nothing is rendered.
		/// </summary>
		private static EndpointDashState GetEndpointDashState(
			float pathLength, float[] dashValues, float dashOffset)
		{
			const float tolerance = 0.1f; // MIN_DASH_ARRAY_LENGTH from WinUI

			var totalPattern = 0f;
			for (int i = 0; i < dashValues.Length; i++)
			{
				totalPattern += dashValues[i];
			}

			if (totalPattern <= 0)
			{
				return EndpointDashState.InRenderedDash;
			}

			var patternStart = -(dashOffset % totalPattern);
			if (patternStart > 0)
			{
				patternStart -= totalPattern;
			}

			var pos = patternStart;
			var idx = 0;

			while (pos < pathLength)
			{
				var segLen = dashValues[idx % dashValues.Length];

				// Guard: skip zero/negative segments so pos always advances.
				// The totalPattern <= 0 check above handles the all-zeros case.
				if (segLen <= 0)
				{
					idx++;
					continue;
				}

				var segEnd = pos + segLen;
				var isDash = (idx % 2) == 0;

				if (segEnd >= pathLength - tolerance)
				{
					if (isDash && pos < pathLength)
					{
						return EndpointDashState.InRenderedDash;
					}

					if (!isDash && MathF.Abs(segEnd - pathLength) < tolerance)
					{
						return EndpointDashState.AtGapBoundary;
					}

					return EndpointDashState.InGap;
				}

				pos = segEnd;
				idx++;
			}

			return EndpointDashState.InGap;
		}

		/// <summary>
		/// Builds a cap shape at the given position extending in the given direction.
		/// </summary>
		private static SKPath? BuildCapPath(SKPoint position, SKPoint direction, float strokeWidth, StrokeCap capType)
		{
			var halfWidth = strokeWidth / 2;
			// Normal perpendicular to direction
			var normal = new SKPoint(-direction.Y, direction.X);

			if (capType == StrokeCap.Round)
			{
				var builder = new SKPathBuilder();
				// Build a semicircle oriented in the cap direction
				var startAngle = (float)(Math.Atan2(normal.Y, normal.X) * 180 / Math.PI);
				var rect = new SKRect(
					position.X - halfWidth,
					position.Y - halfWidth,
					position.X + halfWidth,
					position.Y + halfWidth);
				builder.AddArc(rect, startAngle, -180);
				builder.Close();
				return builder.Detach();
			}
			else if (capType == StrokeCap.Square)
			{
				var builder = new SKPathBuilder();
				// Rectangle extending halfWidth beyond endpoint in direction
				var p1 = new SKPoint(position.X + normal.X * halfWidth, position.Y + normal.Y * halfWidth);
				var p2 = new SKPoint(p1.X + direction.X * halfWidth, p1.Y + direction.Y * halfWidth);
				var p3 = new SKPoint(p2.X - normal.X * strokeWidth, p2.Y - normal.Y * strokeWidth);
				var p4 = new SKPoint(position.X - normal.X * halfWidth, position.Y - normal.Y * halfWidth);
				builder.AddPoly(new[] { p1, p2, p3, p4 }, true);
				return builder.Detach();
			}
			else if (capType == StrokeCap.Triangle)
			{
				var builder = new SKPathBuilder();
				// Isoceles triangle: base perpendicular to direction at endpoint, apex at halfWidth in direction
				var base1 = new SKPoint(position.X + normal.X * halfWidth, position.Y + normal.Y * halfWidth);
				var apex = new SKPoint(position.X + direction.X * halfWidth, position.Y + direction.Y * halfWidth);
				var base2 = new SKPoint(position.X - normal.X * halfWidth, position.Y - normal.Y * halfWidth);
				builder.AddPoly(new[] { base1, apex, base2 }, true);
				return builder.Detach();
			}

			return null;
		}

		/// <summary>
		/// Adds clipped miter trapezoids to the stroke fill path for WinUI miter-clip behavior.
		/// WinUI truncates the miter protrusion at the miter limit distance, while Skia falls
		/// back to a full bevel. This method walks the original geometry to find vertices where
		/// the miter exceeded the limit and adds the clipped miter geometry.
		/// </summary>
		private static void AddClippedMiterJoints(
			SKPathBuilder fillPath,
			SKPath originalGeometry,
			float strokeWidth,
			float miterLimit)
		{
			float hw = strokeWidth / 2;

			using var iter = originalGeometry.CreateIterator(false);
			var points = _spareMiterPoints;

			// Per-contour tracking
			SKPoint contourStart = default;
			SKPoint contourFirstOutDir = default;
			bool hasContourFirstOutDir = false;

			// Previous segment's incoming tangent at its endpoint
			SKPoint prevIncoming = default;
			bool hasPrevIncoming = false;
			SKPoint prevEndPoint = default;

			SKPathVerb verb;
			while ((verb = iter.Next(points)) != SKPathVerb.Done)
			{
				switch (verb)
				{
					case SKPathVerb.Move:
						{
							contourStart = points[0];
							prevEndPoint = points[0];
							hasPrevIncoming = false;
							hasContourFirstOutDir = false;
							break;
						}
					case SKPathVerb.Line:
						{
							var start = points[0];
							var end = points[1];
							var dir = NormalizeVector(end.X - start.X, end.Y - start.Y);
							if (dir == default)
							{
								break;
							}

							if (hasPrevIncoming)
							{
								TryAddMiterClipTrapezoid(fillPath, start, prevIncoming, dir, hw, miterLimit);
							}

							if (!hasContourFirstOutDir)
							{
								contourFirstOutDir = dir;
								hasContourFirstOutDir = true;
							}

							prevIncoming = dir;
							hasPrevIncoming = true;
							prevEndPoint = end;
							break;
						}
					case SKPathVerb.Quad:
					case SKPathVerb.Conic:
						{
							var start = points[0];
							var control = points[1];
							var end = points[2];

							var outDir = NormalizeVector(control.X - start.X, control.Y - start.Y);
							if (outDir == default)
							{
								outDir = NormalizeVector(end.X - start.X, end.Y - start.Y);
							}

							if (outDir == default)
							{
								break;
							}

							if (hasPrevIncoming)
							{
								TryAddMiterClipTrapezoid(fillPath, start, prevIncoming, outDir, hw, miterLimit);
							}

							if (!hasContourFirstOutDir)
							{
								contourFirstOutDir = outDir;
								hasContourFirstOutDir = true;
							}

							var inDir = NormalizeVector(end.X - control.X, end.Y - control.Y);
							if (inDir == default)
							{
								inDir = NormalizeVector(end.X - start.X, end.Y - start.Y);
							}

							if (inDir == default)
							{
								break;
							}

							prevIncoming = inDir;
							hasPrevIncoming = true;
							prevEndPoint = end;
							break;
						}
					case SKPathVerb.Cubic:
						{
							var start = points[0];
							var c1 = points[1];
							var c2 = points[2];
							var end = points[3];

							var outDir = NormalizeVector(c1.X - start.X, c1.Y - start.Y);
							if (outDir == default)
							{
								outDir = NormalizeVector(c2.X - start.X, c2.Y - start.Y);
							}

							if (outDir == default)
							{
								outDir = NormalizeVector(end.X - start.X, end.Y - start.Y);
							}

							if (outDir == default)
							{
								break;
							}

							if (hasPrevIncoming)
							{
								TryAddMiterClipTrapezoid(fillPath, start, prevIncoming, outDir, hw, miterLimit);
							}

							if (!hasContourFirstOutDir)
							{
								contourFirstOutDir = outDir;
								hasContourFirstOutDir = true;
							}

							var inDir = NormalizeVector(end.X - c2.X, end.Y - c2.Y);
							if (inDir == default)
							{
								inDir = NormalizeVector(end.X - c1.X, end.Y - c1.Y);
							}

							if (inDir == default)
							{
								inDir = NormalizeVector(end.X - start.X, end.Y - start.Y);
							}

							if (inDir == default)
							{
								break;
							}

							prevIncoming = inDir;
							hasPrevIncoming = true;
							prevEndPoint = end;
							break;
						}
					case SKPathVerb.Close:
						{
							if (hasPrevIncoming && hasContourFirstOutDir)
							{
								float dx = contourStart.X - prevEndPoint.X;
								float dy = contourStart.Y - prevEndPoint.Y;
								float dist = MathF.Sqrt(dx * dx + dy * dy);

								if (dist > 1e-6f)
								{
									// Implicit closing line from prevEndPoint to contourStart
									var closeDir = new SKPoint(dx / dist, dy / dist);

									// Junction at prevEndPoint
									TryAddMiterClipTrapezoid(fillPath, prevEndPoint, prevIncoming, closeDir, hw, miterLimit);

									// Junction at contourStart
									TryAddMiterClipTrapezoid(fillPath, contourStart, closeDir, contourFirstOutDir, hw, miterLimit);
								}
								else
								{
									// Already at contour start, just process the closing junction
									TryAddMiterClipTrapezoid(fillPath, contourStart, prevIncoming, contourFirstOutDir, hw, miterLimit);
								}
							}

							hasPrevIncoming = false;
							hasContourFirstOutDir = false;
							break;
						}
				}
			}
		}

		/// <summary>
		/// For a single vertex, checks if the miter exceeds the limit and adds a clipped
		/// miter trapezoid. Matches WinUI's DoLimitedMiter() algorithm from strokefigure.cpp.
		/// </summary>
		private static void TryAddMiterClipTrapezoid(
			SKPathBuilder fillPath,
			SKPoint vertex,
			SKPoint dIn,
			SKPoint dOut,
			float halfWidth,
			float miterLimit)
		{
			float dot = dIn.X * dOut.X + dIn.Y * dOut.Y;

			// sin(alpha/2) where alpha is the vertex angle
			float sinHalfSq = (1 + dot) / 2;
			if (sinHalfSq <= 0)
			{
				return; // Collinear or reflex
			}

			float sinHalf = MathF.Sqrt(sinHalfSq);

			// Check if miter exceeds limit: 1/sin(a/2) > miterLimit
			if (sinHalf >= 1f / miterLimit)
			{
				return; // Within limit, Skia's full miter is correct
			}

			float cosHalfSq = (1 - dot) / 2;
			if (cosHalfSq <= 1e-12f)
			{
				return; // Nearly straight or degenerate
			}

			float cosHalf = MathF.Sqrt(cosHalfSq);

			// rRatio = (L - sin(a/2)) / cos(a/2) where L is the miter limit
			float rRatio = (miterLimit - sinHalf) / cosHalf;
			if (rRatio <= 0)
			{
				return;
			}

			// Determine which side the miter extends to
			float cross = dIn.X * dOut.Y - dIn.Y * dOut.X;
			if (MathF.Abs(cross) < 1e-6f)
			{
				return; // Nearly collinear
			}

			// Outward normals toward the miter side
			SKPoint nIn, nOut;
			if (cross > 0)
			{
				// Miter is on the right side
				nIn = new SKPoint(dIn.Y, -dIn.X);
				nOut = new SKPoint(dOut.Y, -dOut.X);
			}
			else
			{
				// Miter is on the left side
				nIn = new SKPoint(-dIn.Y, dIn.X);
				nOut = new SKPoint(-dOut.Y, dOut.X);
			}

			// Bevel endpoints (outer offset at vertex)
			var bevelIn = new SKPoint(vertex.X + nIn.X * halfWidth, vertex.Y + nIn.Y * halfWidth);
			var bevelOut = new SKPoint(vertex.X + nOut.X * halfWidth, vertex.Y + nOut.Y * halfWidth);

			// Clip points (extend along offset edges toward would-be miter tip)
			float ext = rRatio * halfWidth;
			var clipIn = new SKPoint(bevelIn.X + dIn.X * ext, bevelIn.Y + dIn.Y * ext);
			var clipOut = new SKPoint(bevelOut.X - dOut.X * ext, bevelOut.Y - dOut.Y * ext);

			fillPath.AddPoly(new[] { bevelIn, clipIn, clipOut, bevelOut }, true);
		}

		private static SKPoint NormalizeVector(float x, float y)
		{
			float len = MathF.Sqrt(x * x + y * y);
			if (len < 1e-6f)
			{
				return default;
			}

			return new SKPoint(x / len, y / len);
		}
	}
}
