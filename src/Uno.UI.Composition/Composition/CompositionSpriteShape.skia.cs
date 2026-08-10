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

		internal override void Paint(in Visual.PaintingSession session)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				if (FillBrush is { } fill && _fillGeometryWithTransformations is { } finalFillGeometryWithTransformations)
				{
					using var fillGeometry = finalFillGeometryWithTransformations.GetFilledGeometry(Geometry?.TrimStart ?? 0f, Geometry?.TrimEnd ?? 0f);

					// A solid colour (a theme/transition background, or a plain colour brush) can fill the geometry
					// directly. Clip-to-shape + fill-rect is equivalent but forces a per-shape clip — a coverage
					// offscreen on the WebGPU backend, ruinous for shape-heavy UI (list items, icons, charts). Only a
					// non-solid brush (gradient/image/surface) needs clip-to-shape + paint-bounds.
					// A rounded-rect background (the RoundedRectFillHint, set by BorderVisual) fills analytically as ONE
					// SDF quad on backends that support it — instead of a tessellated path (stencil + cover). Only for a
					// solid colour with an identity geometry transform; anything else keeps the path.
					var rrHint = _geometryTransform.IsIdentity ? RoundedRectFillHint : null;
					if (Compositor.TryGetEffectiveBackgroundColor(this, out var colorFromTransition))
					{
						if (rrHint is { } h) { session.Session.DrawRoundedRect(h.Rect, h.Radii, WithOpacity(colorFromTransition, session.Opacity), antialias: true); }
						else { session.Session.DrawPath(fillGeometry, WithOpacity(colorFromTransition, session.Opacity), antialias: true); }
					}
					else if (fill is CompositionColorBrush fillColor && fill.CanPaint())
					{
						if (rrHint is { } h) { session.Session.DrawRoundedRect(h.Rect, h.Radii, WithOpacity(fillColor.Color, session.Opacity), antialias: true); }
						else { session.Session.DrawPath(fillGeometry, WithOpacity(fillColor.Color, session.Opacity), antialias: true); }
					}
					else
					{
						session.Session.Save();
						session.Session.ClipPath(fillGeometry, antialias: true);
						fill.TryPaint(session.Session, session.Opacity, finalFillGeometryWithTransformations.Bounds);
						session.Session.Restore();
					}
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					using var strokeGeometry = geometryWithTransformations.GetStrokeFillGeometry(GetStrokeStyle(withTrim: true));

					if (stroke is CompositionColorBrush strokeColor && stroke.CanPaint())
					{
						// Solid stroke: fill the stroke geometry directly (same reasoning as the solid fill above).
						session.Session.DrawPath(strokeGeometry, WithOpacity(strokeColor.Color, session.Opacity), antialias: true);
					}
					else
					{
						session.Session.Save();
						session.Session.ClipPath(strokeGeometry, antialias: true);
						stroke.TryPaint(session.Session, session.Opacity, strokeGeometry.Bounds);
						session.Session.Restore();
					}
				}
			}
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
					using var strokeGeometry = geometryWithTransformations.GetStrokeFillGeometry(GetStrokeStyle(withTrim: false));
					if (strokeGeometry.FillContains(new Vector2((float)point.X, (float)point.Y)))
					{
						return true;
					}
				}
			}
			return false;
		}

		private StrokeStyle GetStrokeStyle(bool withTrim) => new()
		{
			Thickness = StrokeThickness,
			StartCap = ToStrokeCap(StrokeStartCap),
			EndCap = ToStrokeCap(StrokeEndCap),
			DashCap = ToStrokeCap(StrokeDashCap),
			LineJoin = ToStrokeJoin(StrokeLineJoin),
			MiterLimit = StrokeMiterLimit,
			DashArray = StrokeDashArray is { Count: > 0 } dashArray ? dashArray.ToEvenArray() : null,
			DashOffset = StrokeDashOffset,
			TrimStart = withTrim ? (Geometry?.TrimStart ?? 0f) : 0f,
			TrimEnd = withTrim ? (Geometry?.TrimEnd ?? 0f) : 0f,
		};

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