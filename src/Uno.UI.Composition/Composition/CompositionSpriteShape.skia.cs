#nullable enable

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
					if (StrokeDashArray is { Count: > 0 } strokeDashArray)
					{
						strokePaint.PathEffect = SKPathEffect.CreateDash(strokeDashArray.ToEvenArray(), 0);
					}

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

					var hitTestStrokeFillPath = _spareHitTestPath;

					hitTestStrokeFillPath.Rewind();

					geometryWithTransformations.GetFillPath(strokePaint, hitTestStrokeFillPath);
					if (hitTestStrokeFillPath.Contains((float)point.X, (float)point.Y))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
