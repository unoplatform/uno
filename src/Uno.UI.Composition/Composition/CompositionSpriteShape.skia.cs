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
					// So, to get a correct stroke geometry, we first need to scale the geometry, then get the stroke
					// path from it. Then, before drawing we don't want to apply the scaling.
					// Note that we only need to do so if there is a scaling (ie, value different than 1)
					var shouldAdjustStrokeScaling = Scale.X != 1 || Scale.Y != 1;
					var strokePath = geometry;
					if (shouldAdjustStrokeScaling)
					{
						// We don't want to affect the original SKPath, so we create a new one and apply the scaling transformation on it.
						strokePath = new SKPath();
						var transform = SKMatrix.Identity;
						transform.ScaleX = Scale.X;
						transform.ScaleY = Scale.Y;
						geometry.Transform(transform, strokePath);
					}

					// Get the stroke geometry, after scaling has been applied.
					using var strokeGeometry = strokePaint.GetFillPath(strokePath);
					stroke.UpdatePaint(fillPaint, strokeGeometry.Bounds);

					if (shouldAdjustStrokeScaling)
					{
						// Keep the original matrix to set it back after the stroke is drawn.
						var originalMatrix = session.Surface.Canvas.TotalMatrix;

						// This is copy to not modify originalMatrix. We revert the shape scaling on it.
						var matrix = originalMatrix;
						matrix.ScaleX /= Scale.X; // TODO: Unsure if we should set to 1 or divide by Scale.X
						matrix.ScaleY /= Scale.Y; // TODO: Unsure if we should set to 1 or divide by Scale.Y

						// Use the matrix with reverted scaling before drawing, then restore the original matrix.
						session.Surface.Canvas.SetMatrix(matrix);
						session.Surface.Canvas.DrawPath(strokeGeometry, fillPaint);
						session.Surface.Canvas.SetMatrix(originalMatrix);
					}
					else
					{
						session.Surface.Canvas.DrawPath(strokeGeometry, fillPaint);
					}
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
