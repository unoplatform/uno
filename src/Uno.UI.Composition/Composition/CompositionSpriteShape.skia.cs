#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using Uno;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private CompositionGeometry? _fillGeometry;

		private IGeometry? _geometryWithTransformations;
		private IGeometry? _fillGeometryWithTransformations;

		// A transform that gets baked into the geometry without affecting stroke thickness or
		// the canvas. Set by Microsoft.UI.Xaml.Shapes.Shape to apply Stretch sizing — WinUI's
		// Path/Rectangle keep stroke thickness at the declared value regardless of stretch, and
		// this channel lets Uno match that while keeping CompositionShape.Scale/RotationAngle/
		// TransformMatrix as proper Composition API transforms (which DO scale strokes via the
		// canvas, matching WinUI's CompositionSpriteShape).
		private Matrix3x2 _geometryTransform = Matrix3x2.Identity;

		// Set by BorderVisual for a rounded-rect background: lets the fill go through the backend's analytic
		// DrawRoundedRect (one SDF quad) instead of a tessellated path. Null → unchanged path behaviour.
		internal (Rect Rect, Vector4 Radii)? RoundedRectFillHint { get; set; }

		// Set by BorderVisual for a rounded border stroke: the fill goes through DrawRoundedRectBorder (one analytic
		// annulus SDF quad, outer minus inner) instead of a tessellated ring path. Null → unchanged path behaviour.
		internal (Rect Outer, Vector4 OuterRadii, Rect Inner, Vector4 InnerRadii)? RoundedRectBorderHint { get; set; }

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

		internal void SetGeometryTransform(Matrix3x2 transform)
		{
			_geometryTransform = transform;
			RebuildGeometryWithTransformations();
		}

		private void RebuildGeometryWithTransformations()
		{
			if (Geometry?.BuildGeometry() is IGeometry geometry)
			{
				_geometryWithTransformations = _geometryTransform.IsIdentity
					? geometry
					: geometry.Transform(_geometryTransform);
				if (FillGeometry?.BuildGeometry() is IGeometry fillGeometry)
				{
					_fillGeometryWithTransformations = _geometryTransform.IsIdentity
						? fillGeometry
						: fillGeometry.Transform(_geometryTransform);
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
		}

		internal override bool CanPaint() => (FillBrush?.CanPaint() ?? false) || (StrokeBrush?.CanPaint() ?? false);

		private static global::Windows.UI.Color WithOpacity(global::Windows.UI.Color c, float opacity)
			=> opacity >= 1f ? c : global::Windows.UI.Color.FromArgb((byte)(c.A * opacity), c.R, c.G, c.B);

		// radii = (TopLeft, TopRight, BottomRight, BottomLeft) scalars → a RoundRectangle with circular corners.
		private static RoundRectangle ToRoundRect(Rect rect, Vector4 radii) => new()
		{
			Rect = rect,
			TopLeft = new Vector2(radii.X, radii.X),
			TopRight = new Vector2(radii.Y, radii.Y),
			BottomRight = new Vector2(radii.Z, radii.Z),
			BottomLeft = new Vector2(radii.W, radii.W),
		};

		internal override void Paint(in Visual.PaintingSession session)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				if (FillBrush is { } fill && _fillGeometryWithTransformations is { } finalFillGeometryWithTransformations)
				{
					using var fillGeometry = GetTrimmedFilledGeometry(finalFillGeometryWithTransformations, Geometry);

					// A solid colour (a theme/transition background, or a plain colour brush) can fill the geometry
					// directly. Clip-to-shape + fill-rect is equivalent but forces a per-shape clip — a coverage
					// offscreen on the WebGPU backend, ruinous for shape-heavy UI (list items, icons, charts). Only a
					// non-solid brush (gradient/image/surface) needs clip-to-shape + paint-bounds.
					// A rounded-rect background (the RoundedRectFillHint, set by BorderVisual) fills analytically as ONE
					// SDF quad on backends that support it — instead of a tessellated path (stencil + cover). Only for a
					// solid colour with an identity geometry transform; anything else keeps the path.
					var rrHint = _geometryTransform.IsIdentity ? RoundedRectFillHint : null;
					var brHint = _geometryTransform.IsIdentity ? RoundedRectBorderHint : null;
					// A solid colour fill draws analytically when the shape is a rounded-rect background (rrHint, one
					// SDF quad) or a rounded border ring (brHint, one annulus SDF quad); otherwise it fills the path.
					global::Windows.UI.Color? solidFill =
						Compositor.TryGetEffectiveBackgroundColor(this, out var colorFromTransition) ? colorFromTransition :
						fill is CompositionColorBrush fillColor && fill.CanPaint() ? fillColor.Color :
						null;
					if (solidFill is { } sc)
					{
						var oc = WithOpacity(sc, session.Opacity);
						if (brHint is { } b) { session.Session.DrawRoundedRectBorder(b.Outer, b.OuterRadii, b.Inner, b.InnerRadii, oc, antialias: true); }
						else if (rrHint is { } h) { session.Session.DrawRoundedRect(h.Rect, h.Radii, oc, antialias: true); }
						else { session.Session.DrawPath(fillGeometry, oc, antialias: true); }
					}
					// A brush that cannot paint (a fully transparent colour) must not reach the clip-and-fill path
					// below: it would build a tessellated stencil/depth mask per shape per frame only for TryPaint
					// to draw nothing. Transparent fills are common (any Shape given Fill="Transparent"), and on the
					// WebGPU backend that mask is expensive enough to have hung an Intel GPU.
					else if (fill.CanPaint())
					{
						// A non-solid brush (gradient/image/effect) paints by clipping to the shape then filling its
						// bounds. When the shape is a rounded rect (the hint), clip ANALYTICALLY (ClipRoundRect, 0
						// draws) instead of a tessellated path clip (stencil + depth draws per visual).
						session.Session.Save();
						if (rrHint is { } hc) { session.Session.ClipRoundRect(ToRoundRect(hc.Rect, hc.Radii), antialias: true); }
						else if (brHint is { } bc)
						{
							// A rounded border ring also clips analytically: intersect the outer round rect, exclude the
							// inner one. Matters for gradient borders (Fluent's ControlElevationBorderBrush puts one on
							// every Button): a tessellated ring clip costs a stencil mask per visual and defeats coalescing.
							session.Session.ClipRoundRect(ToRoundRect(bc.Outer, bc.OuterRadii), antialias: true);
							if (bc.Inner.Width > 0 && bc.Inner.Height > 0)
							{
								session.Session.ClipRoundRect(ToRoundRect(bc.Inner, bc.InnerRadii), ClipOperation.Difference, antialias: true);
							}
						}
						else { session.Session.ClipPath(fillGeometry, antialias: true); }
						fill.TryPaint(session.Session, session.Opacity, finalFillGeometryWithTransformations.Bounds);
						session.Session.Restore();
					}
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					if (Uno.UI.Composition.Drawing.DrawingCapabilities.NativeStroking
						&& stroke is CompositionColorBrush nativeStrokeColor && stroke.CanPaint()
						&& _strokeDashArray is not { Count: > 0 }
						&& (Geometry?.TrimStart ?? 0f) == 0f && (Geometry?.TrimEnd ?? 0f) == 0f
						&& StrokeLineJoin == CompositionStrokeLineJoin.Miter
						&& StrokeStartCap == CompositionStrokeCap.Flat && StrokeEndCap == CompositionStrokeCap.Flat)
					{
						session.Session.StrokePath(geometryWithTransformations, WithOpacity(nativeStrokeColor.Color, session.Opacity), StrokeThickness, antialias: true);
						return;
					}

					using var strokeGeometry = GetTrimmedStrokeGeometry(geometryWithTransformations);

					if (stroke is CompositionColorBrush strokeColor && stroke.CanPaint())
					{
						// Solid stroke: fill the stroke geometry directly (same reasoning as the solid fill above).
						session.Session.DrawPath(strokeGeometry, WithOpacity(strokeColor.Color, session.Opacity), antialias: true);
					}
					else if (stroke.CanPaint())
					{
						session.Session.Save();
						session.Session.ClipPath(strokeGeometry, antialias: true);
						stroke.TryPaint(session.Session, session.Opacity, strokeGeometry.Bounds);
						session.Session.Restore();
					}
				}
			}
		}

		/// <summary>
		/// The geometry this shape actually covers when painted (filled area ∪ stroke band), in the owning visual's
		/// local space, or null when it paints nothing. Used to build a precise per-visual damage region. The caller
		/// owns and disposes the returned geometry.
		/// </summary>
		internal IGeometry? BuildRenderGeometry()
		{
			if (_geometryWithTransformations is not { } geometryWithTransformations)
			{
				return null;
			}

			IGeometry? result = null;

			if (FillBrush is not null && _fillGeometryWithTransformations is { } fillGeometryWithTransformations)
			{
				result = GetTrimmedFilledGeometry(fillGeometryWithTransformations, Geometry);
			}

			if (StrokeBrush is not null && StrokeThickness > 0)
			{
				var strokeGeometry = GetTrimmedStrokeGeometry(geometryWithTransformations);
				if (result is null)
				{
					result = strokeGeometry;
				}
				else
				{
					var previous = result;
					result = result.Combine(strokeGeometry, GeometryCombineMode.Union);
					previous.Dispose();
					strokeGeometry.Dispose();
				}
			}

			return result;
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Geometry) or nameof(FillGeometry):
					RebuildGeometryWithTransformations();
					break;
			}
		}

		internal override bool HitTest(Point point)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				point = CombinedTransformMatrix.Inverse().Transform(point);

				if (FillBrush is { } && geometryWithTransformations.FillContains(new Vector2((float)point.X, (float)point.Y)))
				{
					return true;
				}

				if (StrokeBrush is { } && StrokeThickness > 0)
				{
					using var strokeGeometry = geometryWithTransformations.GetStrokeFillGeometry(GetStrokeStyle(0f, 0f));
					if (strokeGeometry.FillContains(new Vector2((float)point.X, (float)point.Y)))
					{
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Bounds of what this shape actually paints, in its parent visual's coordinates; false when it paints
		/// nothing. Used to bound the visual's damage to its shapes instead of falling back to its clip.
		/// </summary>
		internal bool TryGetRenderBounds(out Rect bounds)
		{
			bounds = default;
			var any = false;

			if ((FillBrush?.CanPaint() ?? false) && _fillGeometryWithTransformations is { } fillGeometry)
			{
				bounds = fillGeometry.Bounds;
				any = true;
			}

			if ((StrokeBrush?.CanPaint() ?? false) && StrokeThickness > 0 && _geometryWithTransformations is { } strokeGeometry)
			{
				// The stroke straddles the geometry, so it reaches half a thickness out; a full thickness also
				// covers what joins and caps add beyond that.
				var b = strokeGeometry.Bounds;
				var strokeBounds = new Rect(
					b.X - StrokeThickness,
					b.Y - StrokeThickness,
					b.Width + 2 * StrokeThickness,
					b.Height + 2 * StrokeThickness);

				if (any)
				{
					bounds.Union(strokeBounds);
				}
				else
				{
					bounds = strokeBounds;
					any = true;
				}
			}

			if (any && GetRenderTransform() is { IsIdentity: false } m)
			{
				bounds = bounds.Transform(m);
			}

			return any;
		}

		// What CompositionShape.Render applies to the session before painting: the shape's CombinedTransformMatrix
		// (Scale/Rotation/TransformMatrix around CenterPoint) and then its Offset, matching the Translate + Concat
		// order there. TryGetRenderBounds must apply it too so damage matches the painted pixels.
		private Matrix3x2 GetRenderTransform()
		{
			var transform = CombinedTransformMatrix;
			var offset = Offset;
			return offset == Vector2.Zero ? transform : transform * Matrix3x2.CreateTranslation(offset);
		}

		private StrokeStyle GetStrokeStyle(float trimStart, float trimEnd) => new()
		{
			Thickness = StrokeThickness,
			StartCap = ToStrokeCap(StrokeStartCap),
			EndCap = ToStrokeCap(StrokeEndCap),
			DashCap = ToStrokeCap(StrokeDashCap),
			LineJoin = ToStrokeJoin(StrokeLineJoin),
			MiterLimit = StrokeMiterLimit,
			DashArray = _strokeDashArray is { Count: > 0 } dashArray ? dashArray.ToEvenArray() : null,
			DashOffset = StrokeDashOffset,
			TrimStart = trimStart,
			TrimEnd = trimEnd,
		};

		/// <summary>Strokes <paramref name="geometry"/> through the resolved trim window (see <see cref="TryResolveTrim"/>).</summary>
		private IGeometry GetTrimmedStrokeGeometry(IGeometry geometry)
		{
			if (!TryResolveTrim(Geometry, out var start, out var end, out var wrapEnd))
			{
				return geometry.GetStrokeFillGeometry(GetStrokeStyle(0f, 0f));
			}

			var trimmed = geometry.GetStrokeFillGeometry(GetStrokeStyle(start, end));
			if (wrapEnd is not { } wrapped)
			{
				return trimmed;
			}

			using (trimmed)
			using (var head = geometry.GetStrokeFillGeometry(GetStrokeStyle(0f, wrapped)))
			{
				return trimmed.Combine(head, GeometryCombineMode.Union);
			}
		}

		/// <summary>
		/// Resolves the trim window to [TrimStart + TrimOffset, TrimEnd + TrimOffset] taken modulo 1, returning
		/// false when no trimming is active (the whole path is drawn). A window that the offset pushes past 1.0
		/// wraps: WinUI draws the union of [start, 1] and [0, <paramref name="wrapEnd"/>], not the complement a
		/// single reversed window would give.
		/// </summary>
		private static bool TryResolveTrim(CompositionGeometry? geometry, out float start, out float end, out float? wrapEnd)
		{
			start = 0f;
			end = 0f;
			wrapEnd = null;

			if (geometry is not { } g || (g.TrimStart == default && g.TrimEnd == default && g.TrimOffset == default))
			{
				return false;
			}

			if (g.TrimOffset == default)
			{
				start = g.TrimStart;
				end = g.TrimEnd;
				return true;
			}

			// A full (or wider) window stays full regardless of offset — wrapping its endpoints into [0,1)
			// would otherwise collapse it to nothing.
			if (g.TrimEnd - g.TrimStart >= 1f)
			{
				end = 1f;
				return true;
			}

			start = Wrap01(g.TrimStart + g.TrimOffset);
			end = Wrap01(g.TrimEnd + g.TrimOffset);

			if (start > end)
			{
				wrapEnd = end;
				end = 1f;
			}

			return true;
		}

		private static float Wrap01(float value)
		{
			value %= 1f;
			return value < 0f ? value + 1f : value;
		}

		private static IGeometry GetTrimmedFilledGeometry(IGeometry geometry, CompositionGeometry? source)
		{
			if (!TryResolveTrim(source, out var start, out var end, out var wrapEnd))
			{
				return geometry.GetFilledGeometry(0f, 0f);
			}

			var trimmed = geometry.GetFilledGeometry(start, end);
			if (wrapEnd is not { } wrapped)
			{
				return trimmed;
			}

			using (trimmed)
			using (var head = geometry.GetFilledGeometry(0f, wrapped))
			{
				return trimmed.Combine(head, GeometryCombineMode.Union);
			}
		}

		private static StrokeCap ToStrokeCap(CompositionStrokeCap cap) => cap switch
		{
			CompositionStrokeCap.Square => StrokeCap.Square,
			CompositionStrokeCap.Round => StrokeCap.Round,
			CompositionStrokeCap.Triangle => StrokeCap.Triangle,
			_ => StrokeCap.Butt, // Flat
		};

		private static StrokeJoin ToStrokeJoin(CompositionStrokeLineJoin join) => join switch
		{
			CompositionStrokeLineJoin.Bevel => StrokeJoin.Bevel,
			CompositionStrokeLineJoin.Round => StrokeJoin.Round,
			CompositionStrokeLineJoin.MiterOrBevel => StrokeJoin.MiterOrBevel,
			_ => StrokeJoin.Miter,
		};

	}
}
