#nullable enable

using SkiaSharp;
using System;

namespace Windows.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private SKPaint? _strokePaint;
		private SKPaint? _fillPaint;

		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			if (Geometry is CompositionPathGeometry cpg)
			{
				if (cpg.Path?.GeometrySource is SkiaGeometrySource2D geometrySource)
				{
					if(FillBrush != null)
					{
						FillBrush.UpdatePaint(TryCreateFillPaint());

						surface.Canvas.DrawPath(geometrySource.Geometry, _fillPaint);
					}

					if (StrokeBrush != null && StrokeThickness > 0)
					{
						var strokePaint = TryCreateStrokePaint();

						if (StrokeBrush is CompositionColorBrush stroke)
						{
							strokePaint.StrokeWidth = StrokeThickness;
							strokePaint.Color = stroke.Color.ToSKColor(Compositor.CurrentOpacity);
						}

						surface.Canvas.DrawPath(geometrySource.Geometry, _strokePaint);
					}
				}
				else if(cpg.Path?.GeometrySource is null)
				{

				}
				else
				{
					throw new InvalidOperationException($"CompositionPath does not support the {cpg.Path?.GeometrySource} geometry source");
				}
			}
		}


		private SKPaint TryCreateStrokePaint()
		{
			if (_strokePaint == null)
			{
				_strokePaint = new SKPaint();
				_strokePaint.IsStroke = true;
				_strokePaint.IsAntialias = true;
				_strokePaint.IsAutohinted = true;
			}

			return _strokePaint;
		}
		private SKPaint TryCreateFillPaint()
		{
			if (_fillPaint == null)
			{
				_fillPaint = new SKPaint();
				_fillPaint.IsStroke = false;
				_fillPaint.IsAntialias = true;
				_fillPaint.IsAutohinted = true;
			}

			return _fillPaint;
		}
	}
}
