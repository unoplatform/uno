#nullable enable

using Windows.Foundation;
using SkiaSharp;
using Uno;
using Uno.Extensions;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private SKPaint? _strokePaint;
		private SKPaint? _fillPaint;
		private SKPath? _geometryWithTransformations;

		internal override void Paint(in Visual.PaintingSession session)
		{
			if (_geometryWithTransformations is { } geometryWithTransformations)
			{
				if (FillBrush is { } fill)
				{
					var fillPaint = TryCreateAndClearFillPaint(session.Filters.OpacityColorFilter);

					if (Compositor.TryGetEffectiveBackgroundColor(this, out var colorFromTransition))
					{
						fillPaint.Color = colorFromTransition.ToSKColor();
					}
					else
					{
						fill.UpdatePaint(fillPaint, geometryWithTransformations.Bounds);
					}

					if (fill is CompositionBrushWrapper wrapper)
					{
						fill = wrapper.WrappedBrush;
					}

					if (fill is CompositionEffectBrush { HasBackdropBrushInput: true })
					{
						// workaround until SkiaSharp adds support for SaveLayerRec
						fillPaint.FilterQuality = SKFilterQuality.High;
						// Here, we need to draw directly on the surface's canvas, otherwise
						// you can get an AccessViolationException (most likely because DrawSurface needs the
						// receiver SKCanvas object to be the same as the first argument's SKSurface.Canvas).
						// session.Canvas is possibly a RecorderCanvas from an SKPictureRecorder, so we bypass
						// it and use Surface.Canvas.
						var sessionCanvas = session.Surface.Canvas;
						sessionCanvas.SaveLayer(fillPaint);
						sessionCanvas.Scale(1.0f / sessionCanvas.TotalMatrix.ScaleX);
						sessionCanvas.DrawSurface(session.Surface, new(-sessionCanvas.TotalMatrix.TransX, -sessionCanvas.DeviceClipBounds.Top + sessionCanvas.LocalClipBounds.Top));
						sessionCanvas.Restore();
					}
					else
					{
						session.Canvas.DrawPath(geometryWithTransformations, fillPaint);
					}
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					var fillPaint = TryCreateAndClearFillPaint(session.Filters.OpacityColorFilter);
					var strokePaint = TryCreateAndClearStrokePaint(session.Filters.OpacityColorFilter);

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

					// Get the stroke geometry, after scaling has been applied.
					var strokeGeometry = strokePaint.GetFillPath(geometryWithTransformations);

					stroke.UpdatePaint(fillPaint, strokeGeometry.Bounds);

					session.Canvas.DrawPath(strokeGeometry, fillPaint);
				}
			}
		}

		private SKPaint TryCreateAndClearStrokePaint(SKColorFilter? colorFilter)
			=> TryCreateAndClearPaint(ref _strokePaint, true, colorFilter, CompositionConfiguration.UseBrushAntialiasing);

		private SKPaint TryCreateAndClearFillPaint(SKColorFilter? colorFilter)
			=> TryCreateAndClearPaint(ref _fillPaint, false, colorFilter, CompositionConfiguration.UseBrushAntialiasing);

		private static SKPaint TryCreateAndClearPaint(ref SKPaint? paint, bool isStroke, SKColorFilter? colorFilter, bool isHighQuality = false)
		{
			if (paint == null)
			{
				// Initialization
				paint = new SKPaint();
				paint.IsStroke = isStroke;
				paint.IsAntialias = true;
				paint.IsAutohinted = true;

				if (isHighQuality)
				{
					paint.FilterQuality = SKFilterQuality.High;
				}
			}
			else
			{
				// Cleanup
				// - Brushes can change, we cant leave color and shader garbage
				//	 from last rendering around for the next pass.
				paint.Color = SKColors.White;   // Transparent color wouldn't draw anything
				if (paint.Shader is { } shader)
				{
					shader.Dispose();
					paint.Shader = null;
				}

				if (paint.PathEffect is { } pathEffect)
				{
					pathEffect.Dispose();
					paint.PathEffect = null;
				}
			}

			paint.ColorFilter = colorFilter;

			return paint;
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

			switch (propertyName)
			{
				case nameof(Geometry) or nameof(CombinedTransformMatrix):
					if (Geometry?.BuildGeometry() is SkiaGeometrySource2D { Geometry: { } geometry })
					{
						var transform = CombinedTransformMatrix;
						SKPath geometryWithTransformations;
						if (transform.IsIdentity)
						{
							geometryWithTransformations = geometry;
						}
						else
						{
							geometryWithTransformations = new SKPath();
							geometry.Transform(transform.ToSKMatrix(), geometryWithTransformations);
						}

						_geometryWithTransformations = geometryWithTransformations;
					}
					else
					{
						_geometryWithTransformations = null;
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

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					var strokePaint = TryCreateAndClearStrokePaint(null);
					strokePaint.StrokeWidth = StrokeThickness;
					var strokeGeometry = strokePaint.GetFillPath(geometryWithTransformations);
					if (strokeGeometry.Contains((float)point.X, (float)point.Y))
					{
						return true;
					}
				}
			}
			return false;
		}
	}
}
