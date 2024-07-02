#nullable enable

using System;
using System.Linq;
using SkiaSharp;
using Uno;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Windows.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private SKPaint? _strokePaint;
		private SKPaint? _fillPaint;

		internal override void Paint(in Visual.PaintingSession session)
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
					fill.UpdatePaint(fillPaint, geometryWithTransformations.Bounds);

					if (FillBrush is CompositionEffectBrush { HasBackdropBrushInput: true })
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
						session.Canvas.DrawPath(geometryWithTransformations, fillPaint);
					}
				}

				if (StrokeBrush is { } stroke && StrokeThickness > 0)
				{
					var fillPaint = TryCreateAndClearFillPaint(in session);
					var strokePaint = TryCreateAndClearStrokePaint(in session);

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
					using var strokeGeometry = strokePaint.GetFillPath(geometryWithTransformations);

					stroke.UpdatePaint(fillPaint, strokeGeometry.Bounds);

					session.Canvas.DrawPath(strokeGeometry, fillPaint);
				}
			}
		}

		private SKPaint TryCreateAndClearStrokePaint(in Visual.PaintingSession session)
			=> TryCreateAndClearPaint(in session, ref _strokePaint, true, CompositionConfiguration.UseBrushAntialiasing);

		private SKPaint TryCreateAndClearFillPaint(in Visual.PaintingSession session)
			=> TryCreateAndClearPaint(in session, ref _fillPaint, false, CompositionConfiguration.UseBrushAntialiasing);

		private static SKPaint TryCreateAndClearPaint(in Visual.PaintingSession session, ref SKPaint? paint, bool isStroke, bool isHighQuality = false)
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
				if (paint.Shader != null)
				{
					paint.Shader.Dispose();
					paint.Shader = null;
				}

				if (paint.PathEffect != null)
				{
					paint.PathEffect.Dispose();
					paint.PathEffect = null;
				}
			}

			paint.ColorFilter = session.Filters.OpacityColorFilter;

			return paint;
		}
	}
}
