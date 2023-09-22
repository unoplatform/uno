#nullable enable

using SkiaSharp;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private SKPaint? _strokePaint;
		private SKPaint? _fillPaint;

		internal override void Draw(in DrawingSession session)
		{
			if (Geometry?.BuildGeometry() is SkiaGeometrySource2D { Geometry: { } geometry })
			{
				var transform = this.GetTransform();
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

				if (FillBrush is { } fill)
				{
					var fillPaint = TryCreateAndClearFillPaint(in session);

					fill.UpdatePaint(fillPaint, geometry.Bounds);

					if (FillBrush is CompositionEffectBrush { HasBackdropBrushInput: true })
					{
						// workaround until SkiaSharp adds support for SaveLayerRec
						session.Surface.Canvas.SaveLayer(fillPaint);
						session.Surface.Canvas.Scale(1.0f / session.Surface.Canvas.TotalMatrix.ScaleX);
						session.Surface.Canvas.DrawSurface(session.Surface, new(-session.Surface.Canvas.TotalMatrix.TransX, -session.Surface.Canvas.DeviceClipBounds.Top + session.Surface.Canvas.LocalClipBounds.Top));
						session.Surface.Canvas.Restore();
					}
					else
					{
						session.Surface.Canvas.DrawPath(geometryWithTransformations, fillPaint);
					}
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
					// So, to get a correct stroke geometry, we must apply the transformations first.

					// Get the stroke geometry, after scaling has been applied.
					using var strokeGeometry = strokePaint.GetFillPath(geometryWithTransformations);

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
