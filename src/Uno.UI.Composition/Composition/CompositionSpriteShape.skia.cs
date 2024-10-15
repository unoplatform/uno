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

		// TODO: profile the impact of this optimization and consider removing it
		// It's hacky and can break if SkiaSharp exposes new properties that
		// are then used and modified in CompositionBrush.UpdatePaint().
		private static SKPaint TryCreateAndClearPaint(ref SKPaint? paint, bool isStroke, SKColorFilter? colorFilter, bool isHighQuality = false)
		{
			if (paint == null)
			{
				paint = new SKPaint();
			}
			else
			{
				// defaults
				paint.IsAntialias = false;
				paint.BlendMode = SKBlendMode.SrcOver;
				paint.FakeBoldText = false;
				paint.HintingLevel = SKPaintHinting.Normal;
				paint.IsDither = false;
				paint.IsEmbeddedBitmapText = false;
				paint.IsLinearText = false;
				paint.LcdRenderText = false;
				paint.StrokeCap = SKStrokeCap.Butt;
				paint.StrokeJoin = SKStrokeJoin.Miter;
				paint.StrokeMiter = 4;
				paint.StrokeWidth = 0;
				paint.SubpixelText = false;
				paint.TextAlign = SKTextAlign.Left;
				paint.TextEncoding = SKTextEncoding.Utf8;
				paint.TextScaleX = 1;
				paint.TextSize = 12;

				// Cleanup
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

				if (paint.ImageFilter is { } imageFilter)
				{
					imageFilter.Dispose();
					paint.ImageFilter = null;
				}

				if (paint.MaskFilter is { } maskFilter)
				{
					maskFilter.Dispose();
					paint.MaskFilter = null;
				}

				if (paint.Typeface is { } typeface)
				{
					typeface.Dispose();
					paint.Typeface = null;
				}
			}

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
