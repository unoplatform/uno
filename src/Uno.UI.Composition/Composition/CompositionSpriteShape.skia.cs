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

		private static SKPaint _sparePaintFillPaint = new SKPaint();
		private static SKPaint _sparePaintStrokePaint = new SKPaint();
		private static SKPath _sparePaintPath = new SKPath();

		internal override void Paint(in Visual.PaintingSession session)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				if (FillBrush is { } fill && _fillGeometryWithTransformations is { } finalFillGeometryWithTransformations)
				{
					var fillPaint = _sparePaintFillPaint;

					fillPaint.Reset();

					PrepareTempPaint(fillPaint, isStroke: false, session.OpacityColorFilter);

					if (Compositor.TryGetEffectiveBackgroundColor(this, out var colorFromTransition))
					{
						fillPaint.Color = colorFromTransition.ToSKColor();
					}
					else
					{
						fill.UpdatePaint(fillPaint, finalFillGeometryWithTransformations.Bounds);
					}

					if (fill is CompositionBrushWrapper wrapper)
					{
						fill = wrapper.WrappedBrush;
					}

					if (Geometry is not null && (Geometry.TrimStart != default || Geometry.TrimEnd != default))
					{
						fillPaint.PathEffect = SKPathEffect.CreateTrim(Geometry.TrimStart, Geometry.TrimEnd);
					}

					if (fill is CompositionEffectBrush { HasBackdropBrushInput: true })
					{
						session.Canvas.SaveLayer(new SKCanvasSaveLayerRec { Backdrop = fillPaint.ImageFilter });
						session.Canvas.Restore();
					}
					else
					{
						finalFillGeometryWithTransformations.CanvasDrawPath(session.Canvas, fillPaint);
					}
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					var fillPaint = _sparePaintFillPaint;
					var strokePaint = _sparePaintStrokePaint;

					fillPaint.Reset();
					strokePaint.Reset();

					PrepareTempPaint(fillPaint, isStroke: false, session.OpacityColorFilter);
					PrepareTempPaint(strokePaint, isStroke: true, session.OpacityColorFilter);

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

					var strokeFillPath = _sparePaintPath;

					strokeFillPath.Rewind();

					// Get the stroke geometry, after scaling has been applied.
					geometryWithTransformations.GetFillPath(strokePaint, strokeFillPath);

					stroke.UpdatePaint(fillPaint, strokeFillPath.Bounds);

					session.Canvas.DrawPath(strokeFillPath, fillPaint);
				}
			}
		}

		private static void PrepareTempPaint(SKPaint paint, bool isStroke, SKColorFilter? colorFilter)
		{
			paint.IsAntialias = true;
			paint.ColorFilter = colorFilter;

			paint.IsStroke = isStroke;

			// uno-specific defaults
			paint.Color = SKColors.White;   // Transparent color wouldn't draw anything
			paint.IsAntialias = true;

			paint.ColorFilter = colorFilter;
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

		private static SKPaint _spareHitTestPaint = new SKPaint();
		private static SKPath _spareHitTestPath = new SKPath();

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

					strokePaint.Reset();

					PrepareTempPaint(strokePaint, isStroke: true, null);

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
