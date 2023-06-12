#nullable enable

using SkiaSharp;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private SKPaint? _strokePaint;
		private SKPaint? _fillPaint;

		internal override void Draw(in DrawingSession session)
		{
			if (Geometry?.BuildGeometry() is SkiaGeometrySource2D { Geometry: { } geometry })
			{
				if (FillBrush is { } fill)
				{
					var fillPaint = TryCreateAndClearFillPaint(in session);

					fill.UpdatePaint(fillPaint, geometry.Bounds);

					session.Surface.Canvas.DrawPath(geometry, fillPaint);
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					var fillPaint = TryCreateAndClearFillPaint(in session);
					var strokePaint = TryCreateAndClearStrokePaint(in session);

					// Set stroke thickness
					strokePaint.StrokeWidth = StrokeThickness;
					// TODO: Add support for dashes here
					// strokePaint.PathEffect = SKPathEffect.CreateDash();

					// Generate stroke geometry for bounds that will be passed to a brush.
					// - [Future]: This generated geometry should also be used for hit testing.
					using var strokeGeometry = strokePaint.GetFillPath(geometry);

					stroke.UpdatePaint(fillPaint, strokeGeometry.Bounds);

					session.Surface.Canvas.DrawPath(strokeGeometry, fillPaint);
				}
			}
		}

		private SKPaint TryCreateAndClearStrokePaint(in DrawingSession session) 
			=> TryCreateAndClearPaint(in session, ref _strokePaint, true);

		private SKPaint TryCreateAndClearFillPaint(in DrawingSession session) 
			=> TryCreateAndClearPaint(in session, ref _fillPaint, false);

		private static SKPaint TryCreateAndClearPaint(in DrawingSession session, ref SKPaint? paint, bool isStroke)
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

			paint.ColorFilter = session.Filters.OpacityColorFilter;

			return paint;
		}
	}
}
