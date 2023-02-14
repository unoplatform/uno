#nullable enable

using SkiaSharp;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private SKPaint? _strokePaint;
		private SKPaint? _fillPaint;

		internal override void Render(SKSurface surface)
		{
			SkiaGeometrySource2D? geometrySource = Geometry?.BuildGeometry() as SkiaGeometrySource2D;

			SKPath? geometry = geometrySource?.Geometry;
			if (geometry == null)
			{
				return;
			}

			if (FillBrush != null)
			{
				var fillPaint = TryCreateAndClearFillPaint();

				FillBrush.UpdatePaint(fillPaint, geometry.Bounds);

				surface.Canvas.DrawPath(geometry, fillPaint);
			}

			if (StrokeBrush != null && StrokeThickness > 0)
			{
				var fillPaint = TryCreateAndClearFillPaint();
				var strokePaint = TryCreateAndClearStrokePaint();

				// Set stroke thickness
				strokePaint.StrokeWidth = StrokeThickness;
				// TODO: Add support for dashes here
				// strokePaint.PathEffect = SKPathEffect.CreateDash();

				// Generate stroke geometry for bounds that will be passed to a brush.
				// - [Future]: This generated geometry should also be used for hit testing.
				using (var strokeGeometry = strokePaint.GetFillPath(geometry))
				{
					StrokeBrush.UpdatePaint(fillPaint, strokeGeometry.Bounds);

					surface.Canvas.DrawPath(strokeGeometry, fillPaint);
				}
			}
		}

		private SKPaint TryCreateAndClearStrokePaint() => TryCreateAndClearPaint(ref _strokePaint, true);

		private SKPaint TryCreateAndClearFillPaint() => TryCreateAndClearPaint(ref _fillPaint, false);

		private SKPaint TryCreateAndClearPaint(ref SKPaint? paint, bool isStroke)
		{
			if (paint == null)
			{
				// Initialization
				paint = new SKPaint();
				paint.IsStroke = isStroke;
				paint.IsAntialias = true;
				paint.IsAutohinted = true;
			}
			else
			{
				// Cleanup
				// - Brushes can change, we cant leave color and shader garbage
				//	 from last rendering around for the next pass.
				paint.Color = SKColors.White;   // Transparent color wouldnt draw anything
				if (paint.Shader != null)
				{
					paint.Shader.Dispose();
					paint.Shader = null;
				}
			}

			paint.ColorFilter = Compositor.CurrentOpacityColorFilter;

			return paint;
		}
	}
}
