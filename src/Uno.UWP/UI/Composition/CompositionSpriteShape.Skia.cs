#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using SkiaSharp;
using System;

namespace Windows.UI.Composition
{
	public partial class CompositionSpriteShape : CompositionShape
	{
		private SKPaint _strokePaint;
		private SKPaint _fillPaint;

		public CompositionSpriteShape()
		{
		}

		internal override void Render(SKSurface surface, SKImageInfo info)
		{
			if (Geometry is CompositionPathGeometry cpg)
			{
				if (cpg.Path.GeometrySource is SkiaGeometrySource2D geometrySource)
				{
					if(FillBrush != null)
					{
						TryCreateFillPaint();

						if (FillBrush is CompositionColorBrush fill)
						{
							_fillPaint.Color = fill.Color.ToSKColor();
						}

						surface.Canvas.DrawPath(geometrySource.Geometry, _fillPaint);
					}

					if (StrokeBrush != null)
					{
						TryCreateStrokePaint();

						if (StrokeBrush is CompositionColorBrush stroke)
						{
							_strokePaint.StrokeWidth = StrokeThickness;
							_strokePaint.Color = stroke.Color.ToSKColor();
						}

						surface.Canvas.DrawPath(geometrySource.Geometry, _strokePaint);
					}
				}
				else
				{
					throw new InvalidOperationException($"CompositionPath does not support the {cpg.Path?.GeometrySource} geometry source");
				}
			}
		}

		private void TryCreateStrokePaint()
		{
			if (_strokePaint == null)
			{
				_strokePaint = new SKPaint();
				_strokePaint.IsStroke = true;
				_strokePaint.IsAntialias = true;
				_strokePaint.IsAutohinted = true;
			}
		}
		private void TryCreateFillPaint()
		{
			if (_fillPaint == null)
			{
				_fillPaint = new SKPaint();
				_fillPaint.IsStroke = false;
				_fillPaint.IsAntialias = true;
				_fillPaint.IsAutohinted = true;
			}
		}
	}
}
