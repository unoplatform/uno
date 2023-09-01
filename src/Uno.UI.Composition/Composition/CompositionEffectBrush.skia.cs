using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
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

						return null;
					}
				case IGraphicsEffectD2D1Interop effectInterop:
					{
						switch (EffectHelpers.GetEffectType(effectInterop.GetEffectId()))
						{
							case EffectType.GaussianBlurEffect:
								{
									if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 3 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
									{
										SKImageFilter sourceFilter = GenerateEffectFilter(source, bounds);
										if (sourceFilter is null)
											return null;
										effectInterop.GetNamedPropertyMapping("BlurAmount", out uint sigmaProp, out _);
										effectInterop.GetNamedPropertyMapping("Optimization", out uint optProp, out _);
										effectInterop.GetNamedPropertyMapping("BorderMode", out uint borderProp, out _);

										// TODO: Implement support for other GraphicsEffectPropertyMapping values than Direct
										float sigma = (float)effectInterop.GetProperty(sigmaProp);
										_ = (uint)effectInterop.GetProperty(optProp); // TODO
										_ = (uint)effectInterop.GetProperty(borderProp); // TODO

										return SKImageFilter.CreateBlur(sigma, sigma, sourceFilter, new(bounds));
									}

									return null;
								}
							case EffectType.Unsupported:
							default:
								return null;
						}
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
