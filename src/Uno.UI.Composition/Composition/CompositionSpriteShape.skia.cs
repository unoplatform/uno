#nullable enable

using Windows.Foundation;
using SkiaSharp;
using Uno;
using Uno.Disposables;
using Uno.Extensions;

namespace Windows.UI.Composition
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

		internal override void Paint(in Visual.PaintingSession session)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				if (FillBrush is { } fill && _fillGeometryWithTransformations is { } finalFillGeometryWithTransformations)
				{
					using var fillPaintDisposable = GetTempFillPaint(session.Filters.OpacityColorFilter, out var fillPaint);

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

					if (fill is CompositionEffectBrush { HasBackdropBrushInput: true })
					{
						// workaround until SkiaSharp adds support for SaveLayerRec
						fillPaint.FilterQuality = SKFilterQuality.High;
						session.Canvas.SaveLayer(fillPaint);
						session.Canvas.Scale(1.0f / session.Canvas.TotalMatrix.ScaleX);
						session.Canvas.DrawSurface(session.Surface, new(-session.Canvas.TotalMatrix.TransX, -session.Canvas.DeviceClipBounds.Top + session.Canvas.LocalClipBounds.Top));
						session.Canvas.Restore();
					}
					else
					{
						finalFillGeometryWithTransformations.CanvasDrawPath(session.Canvas, fillPaint);
					}
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					using var fillPaintDisposable = GetTempFillPaint(session.Filters.OpacityColorFilter, out var fillPaint);
					using var strokePaintDisposable = GetTempStrokePaint(session.Filters.OpacityColorFilter, out var strokePaint);

					// Set stroke thickness
					strokePaint.StrokeWidth = StrokeThickness;
					if (StrokeDashArray is { Count: > 0 } strokeDashArray)
					{
						strokePaint.PathEffect = SKPathEffect.CreateDash(strokeDashArray.ToEvenArray(), 0);
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

					using (SkiaHelper.GetTempSKPath(out var strokeFillPath))
					{
						// Get the stroke geometry, after scaling has been applied.
						geometryWithTransformations.GetFillPath(strokePaint, strokeFillPath);

						stroke.UpdatePaint(fillPaint, strokeFillPath.Bounds);

						session.Canvas.DrawPath(strokeFillPath, fillPaint);
					}
				}
			}
		}

		private DisposableStruct<SKPaint> GetTempStrokePaint(SKColorFilter? colorFilter, out SKPaint paint)
			=> GetTempPaint(out paint, true, colorFilter, CompositionConfiguration.UseBrushAntialiasing);

		private DisposableStruct<SKPaint> GetTempFillPaint(SKColorFilter? colorFilter, out SKPaint paint)
			=> GetTempPaint(out paint, false, colorFilter, CompositionConfiguration.UseBrushAntialiasing);

		private static DisposableStruct<SKPaint> GetTempPaint(out SKPaint paint, bool isStroke, SKColorFilter? colorFilter, bool isHighQuality = false)
		{
			var disposable = SkiaHelper.GetTempSKPaint(out paint);
			paint.IsAntialias = true;
			paint.ColorFilter = colorFilter;

			paint.IsStroke = isStroke;

			// uno-specific defaults
			paint.Color = SKColors.White;   // Transparent color wouldn't draw anything
			paint.IsAutohinted = true;
			// paint.IsAntialias = true; // IMPORTANT: don't set this to true by default. It breaks canvas clipping on Linux for some reason.

			paint.ColorFilter = colorFilter;
			if (isHighQuality)
			{
				paint.FilterQuality = SKFilterQuality.High;
			}

			return disposable;
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
					using (GetTempStrokePaint(null, out var strokePaint))
					{
						strokePaint.StrokeWidth = StrokeThickness;

						using (SkiaHelper.GetTempSKPath(out var hitTestStrokeFillPath))
						{
							geometryWithTransformations.GetFillPath(strokePaint, hitTestStrokeFillPath);
							if (hitTestStrokeFillPath.Contains((float)point.X, (float)point.Y))
							{
								return true;
							}
						}
					}
				}
			}
			return false;
		}
	}
}
