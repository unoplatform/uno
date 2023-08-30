using Windows.Graphics.Effects;
using System;
using SkiaSharp;

namespace Windows.UI.Composition
{
	public partial class CompositionEffectBrush : CompositionBrush
	{
		private SKImageFilter GenerateEffectFilter(object effect, SKRect bounds)
		{
			// TODO: https://user-images.githubusercontent.com/34550324/264485558-d7ee5062-b0e0-4f6e-a8c7-0620ec561d3d.png

			switch (effect)
			{
				case CompositionEffectSourceParameter effectSourceParameter:
					{
						CompositionBrush brush = GetSourceParameter(effectSourceParameter.Name);
						if (brush is not null)
						{
							SKPaint paint = new SKPaint() { IsAntialias = true, IsAutohinted = true, FilterQuality = SKFilterQuality.High };
							brush.UpdatePaint(paint, bounds);

							return SKImageFilter.CreatePaint(paint, new SKImageFilter.CropRect(bounds));
						}
						else
							return null;
					}
				default:
					return null;
			}
		}

		internal override void UpdatePaint(SKPaint paint, SKRect bounds)
		{
			SKImageFilter filter = GenerateEffectFilter(_effect, bounds);
			if (filter is null)
				throw new NotSupportedException("The specified IGraphicsEffect is not supported"); // TODO: Replicate Windows' behavior

			paint.Shader = null;
			paint.ImageFilter = filter;
			paint.FilterQuality = SKFilterQuality.High;
		}
	}
}
