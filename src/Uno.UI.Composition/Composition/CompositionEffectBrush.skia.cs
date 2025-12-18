#nullable enable

using System;
using SkiaSharp;
using Windows.UI;
using Microsoft.UI;
using System.Numerics;
using Uno.UI.Composition;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Composition;

public partial class CompositionEffectBrush : CompositionBrush
{
	private bool _isCurrentInputBackdrop;
	private bool? _currentCompMode;

	private SKRect _currentBounds;
	private SKImageFilter? _filter;
	private bool _hasBackdropBrushInput;
	private bool _hasBackdropBrushInputPrivate; // this one is reset and set during effect generation and is only copied to _hasBackdropBrushInput once done. This avoids needless invalidations when HasBackdropBrushInput is reset then set immediately.

	internal bool HasBackdropBrushInput
	{
		get => _hasBackdropBrushInput;
		private set => SetProperty(ref _hasBackdropBrushInput, value);
	}

	internal override bool RequiresRepaintOnEveryFrame => HasBackdropBrushInput;

	internal bool UseBlurPadding { get; set; }

	private SKImageFilter? GenerateGaussianBlurEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			// TODO: Support "Optimization" and "BorderMode" properties
			effectInterop.GetNamedPropertyMapping("BlurAmount", out uint sigmaProp, out _);
			float sigma = (float)(effectInterop.GetProperty(sigmaProp) ?? throw new InvalidOperationException("The effect property was null"));

			return SKImageFilter.CreateBlur(sigma, sigma, sourceFilter,
				UseBlurPadding ?
				bounds with
				{
					Left = -100,
					Top = -100,
					Right = bounds.Right + 100,
					Bottom = bounds.Bottom + 100
				}
				: bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateGrayscaleEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // Grayscale Matrix
				{
					0.21f, 0.72f, 0.07f, 0, 0,
					0.21f, 0.72f, 0.07f, 0, 0,
					0.21f, 0.72f, 0.07f, 0, 0,
					0,     0,     0,     1, 0
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateInvertEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // Invert Matrix
				{
					-1, 0,  0,  0, 1,
					0,  -1, 0,  0, 1,
					0,  0,  -1, 0, 1,
					0,  0,  0,  1, 0,
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateHueRotationEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Angle", out uint angleProp, out GraphicsEffectPropertyMapping angleMapping);
			float angle = (float)(effectInterop.GetProperty(angleProp) ?? throw new InvalidOperationException("The effect property was null"));

			if (angleMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
			{
				angle *= 180.0f / MathF.PI;
			}

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // Hue Rotation Matrix
				{
					0.2127f + MathF.Cos(angle) * 0.7873f - MathF.Sin(angle) * 0.2127f, 0.715f - MathF.Cos(angle) * 0.715f - MathF.Sin(angle) * 0.715f, 0.072f - MathF.Cos(angle) * 0.072f + MathF.Sin(angle) * 0.928f, 0, 0,
					0.2127f - MathF.Cos(angle) * 0.213f + MathF.Sin(angle) * 0.143f,   0.715f + MathF.Cos(angle) * 0.285f + MathF.Sin(angle) * 0.140f, 0.072f - MathF.Cos(angle) * 0.072f - MathF.Sin(angle) * 0.283f, 0, 0,
					0.2127f - MathF.Cos(angle) * 0.213f - MathF.Sin(angle) * 0.787f,   0.715f - MathF.Cos(angle) * 0.715f + MathF.Sin(angle) * 0.715f, 0.072f + MathF.Cos(angle) * 0.928f + MathF.Sin(angle) * 0.072f, 0, 0,
					0,                                                                 0,                                                              0,                                                              1, 0
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateTintEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// Reference: https://learn.microsoft.com/en-us/windows/win32/direct2d/tint-effect

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 1 /* only the Color property is required */ && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			if (Compositor.IsSoftwareRenderer is true)
			{
				if (sourceFilter is not null)
				{
					return sourceFilter;
				}
				else
				{
					return SKImageFilter.CreateOffset(0, 0, null, bounds);
				}
			}

			// Note: ColorHdr isn't supported by Composition (as of 10.0.25941.1000)
			effectInterop.GetNamedPropertyMapping("Color", out uint colorProp, out _);
			effectInterop.GetNamedPropertyMapping("ClampOutput", out uint clampProp, out _);

			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool clamp = clampProp != 0xFF ? (bool?)effectInterop.GetProperty(clampProp) ?? false : false;

			string shader =
$$"""
				uniform shader input;
				uniform vec4 color;

				half4 main() 
				{
					return {{(clamp ? "clamp(" : String.Empty)}}sample(input) * color{{(clamp ? ", 0.0, 1.0)" : String.Empty)}};
				}
""";

			SKRuntimeEffect runtimeEffect = SKRuntimeEffect.CreateShader(shader, out string errors);
			if (errors is not null)
			{
				return null;
			}

			SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
			{
				{ "color", new float[] { color.R * (1.0f / 255.0f), color.G * (1.0f / 255.0f), color.B * (1.0f / 255.0f), color.A * (1.0f / 255.0f) } }
			};

			SKRuntimeEffectChildren children = new(runtimeEffect);
			children.Add("input", null);

			return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateBlendEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// TODO: Replace this with a pixel shader to get the same output as Windows

		if (effectInterop.GetSourceCount() == 2 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource bg && effectInterop.GetSource(1) is IGraphicsEffectSource fg)
		{
			SKImageFilter? bgFilter = GenerateEffectFilter(bg, bounds);
			if (bgFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			SKImageFilter? fgFilter = GenerateEffectFilter(fg, bounds);
			if (fgFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Mode", out uint modeProp, out _);
			D2D1BlendEffectMode mode = (D2D1BlendEffectMode)(uint)(effectInterop.GetProperty(modeProp) ?? throw new InvalidOperationException("The effect property was null"));
			SKBlendMode skMode = mode.ToSkia();

			if (skMode == (SKBlendMode)0xFF) // Unsupported mode, fallback to default mode, we can add support for other modes when we move to Skia 3 through pixel shaders
			{
				skMode = SKBlendMode.Multiply;
			}

			return SKImageFilter.CreateBlendMode(skMode, bgFilter, fgFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateCompositeEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() > 1 && effectInterop.GetPropertyCount() == 1)
		{
			SKImageFilter? currentFilter = GenerateEffectFilter(effectInterop.GetSource(0), bounds);
			if (currentFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Mode", out uint modeProp, out _);
			D2D1CompositeMode mode = (D2D1CompositeMode)(uint)(effectInterop.GetProperty(modeProp) ?? throw new InvalidOperationException("The effect property was null"));
			SKBlendMode skMode = mode.ToSkia();

			if (skMode == (SKBlendMode)0xFF) // Unsupported mode, fallback to default mode, we can add support for other modes when we move to Skia 3 through pixel shaders
			{
				skMode = SKBlendMode.SrcOver;
			}

			// We have to do this manually because SKImageFilter.CreateMerge(SKImageFilter, SKImageFilter, SKBlendMode, SKImageFilter.CropRect) is obsolete.
			for (uint idx = 1; idx < effectInterop.GetSourceCount(); idx++)
			{
				SKImageFilter? nextFilter = GenerateEffectFilter(effectInterop.GetSource(idx), bounds);

				if (nextFilter is not null && !_isCurrentInputBackdrop)
				{
					currentFilter = SKImageFilter.CreateBlendMode(skMode, currentFilter, nextFilter, bounds);
				}

				_isCurrentInputBackdrop = false;
			}

			return currentFilter;
		}

		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private SKImageFilter? GenerateColorSourceEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetPropertyCount() >= 1 /* only the Color property is required */)
		{
			// Note: ColorHdr isn't supported by Composition (as of 10.0.25941.1000)
			effectInterop.GetNamedPropertyMapping("Color", out uint colorProp, out _);
			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateBlendMode(color.ToSKColor(), SKBlendMode.Src), null, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateOpacityEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Opacity", out uint opacityProp, out _);
			float opacity = (float)(effectInterop.GetProperty(opacityProp) ?? throw new InvalidOperationException("The effect property was null"));


			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // Opacity Matrix
				{
					1, 0, 0, 0,       0,
					0, 1, 0, 0,       0,
					0, 0, 1, 0,       0,
					0, 0, 0, opacity, 0
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateContrastEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// Reference: https://learn.microsoft.com/en-us/windows/win32/direct2d/contrast-effect

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 1 /* only the Contrast property is required */ && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			if (Compositor.IsSoftwareRenderer is true)
			{
				if (sourceFilter is not null)
				{
					return sourceFilter;
				}
				else
				{
					return SKImageFilter.CreateOffset(0, 0, null, bounds);
				}
			}

			effectInterop.GetNamedPropertyMapping("Contrast", out uint contrastProp, out _);
			effectInterop.GetNamedPropertyMapping("ClampSource", out uint clampProp, out _);

			float contrast = (float)(effectInterop.GetProperty(contrastProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool clamp = clampProp != 0xFF ? (bool?)effectInterop.GetProperty(clampProp) ?? false : false;

			string shader =
$$"""
				uniform shader input;
				uniform half contrastValue;

				half4 Premultiply(half4 color)
				{
					color.rgb *= color.a;
					return color;
				}

				half4 UnPremultiply(half4 color)
				{
					color.rgb = (color.a == 0) ? half3(0, 0, 0) : (color.rgb / color.a);
					return color;
				}

				half4 Contrast(half4 color, half contrast)
				{
					color = UnPremultiply(color);

					half s = 1 - (3.0 / 4.0) * contrast;
					half c2 = s - 1;
					half b2 = 4 - 3 * s;
					half a2 = 2 * c2;
					half b1 = s;
					half a1 = -a2;
    
					half3 lowResult = color.rgb * (color.rgb * a1 + b1);
					half3 highResult = color.rgb * (color.rgb * a2 + b2) + c2;
    
					half3 comparisonResult = half3(0.0);
					comparisonResult.r = (color.rgb.r < 0.5) ? 1.0 : 0.0;
					comparisonResult.g = (color.rgb.g < 0.5) ? 1.0 : 0.0;
					comparisonResult.b = (color.rgb.b < 0.5) ? 1.0 : 0.0;

					color.rgb = mix(lowResult, highResult, comparisonResult);
    
					return Premultiply(color);
				}

				half4 main() 
				{
					return Contrast({{(clamp ? "clamp(" : String.Empty)}}sample(input){{(clamp ? ", 0.0, 1.0)" : String.Empty)}}, contrastValue);
				}
""";

			SKRuntimeEffect runtimeEffect = SKRuntimeEffect.CreateShader(shader, out string errors);
			if (errors is not null)
			{
				return null;
			}

			SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
			{
				{ "contrastValue", contrast }
			};

			SKRuntimeEffectChildren children = new(runtimeEffect);
			children.Add("input", null);

			return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateArithmeticCompositeEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// TODO: Support "ClampOutput" property

		if (effectInterop.GetSourceCount() == 2 && effectInterop.GetPropertyCount() >= 4 && effectInterop.GetSource(0) is IGraphicsEffectSource bg && effectInterop.GetSource(1) is IGraphicsEffectSource fg)
		{
			SKImageFilter? bgFilter = GenerateEffectFilter(bg, bounds);
			if (bgFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			SKImageFilter? fgFilter = GenerateEffectFilter(fg, bounds);
			if (fgFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			float? multiplyAmount = effectInterop.GetProperty(0) as float?;
			float? source1Amount = effectInterop.GetProperty(1) as float?;
			float? source2Amount = effectInterop.GetProperty(2) as float?;
			float? offset = effectInterop.GetProperty(3) as float?;

			if (multiplyAmount is null || source1Amount is null || source2Amount is null || offset is null)
			{
				float[]? coefficients = effectInterop.GetProperty(0) as float[];

				if (coefficients is null)
				{
					effectInterop.GetNamedPropertyMapping("Coefficients", out uint coefficientsProp, out GraphicsEffectPropertyMapping coefficientsMapping);
					if (coefficientsMapping == GraphicsEffectPropertyMapping.Direct)
					{
						coefficients = effectInterop.GetProperty(coefficientsProp) as float[];
					}
					else
					{
						return null;
					}
				}

				if (coefficients is not null && coefficients.Length == 4)
				{
					multiplyAmount = coefficients[0];
					source1Amount = coefficients[1];
					source2Amount = coefficients[2];
					offset = coefficients[3];
				}
				else
				{
					return null;
				}
			}

			return SKImageFilter.CreateArithmetic(multiplyAmount.Value, source1Amount.Value, source2Amount.Value, offset.Value, false, bgFilter, fgFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateExposureEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// Reference: https://developer.apple.com/library/archive/documentation/GraphicsImaging/Reference/CoreImageFilterReference/index.html#//apple_ref/doc/filter/ci/CIExposureAdjust
		// Reference: https://stackoverflow.com/a/30483221/11547162

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Exposure", out uint exposureProp, out _);

			float exposure = (float)(effectInterop.GetProperty(exposureProp) ?? throw new InvalidOperationException("The effect property was null"));
			float multiplier = MathF.Pow(2.0f, exposure);

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // Exposure Matrix
				{
					multiplier, 0,          0,          0, 0,
					0,          multiplier, 0,          0, 0,
					0,          0,          multiplier, 0, 0,
					0,          0,          0,          1, 0,
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateCrossFadeEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// TODO: We should use SkColorFilters::Lerp instead once SkiaSharp includes it

		if (effectInterop.GetSourceCount() == 2 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource sourceA && effectInterop.GetSource(1) is IGraphicsEffectSource sourceB)
		{
			SKImageFilter? sourceFilter1 = GenerateEffectFilter(sourceB, bounds);
			if (sourceFilter1 is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			SKImageFilter? sourceFilter2 = GenerateEffectFilter(sourceA, bounds);
			if (sourceFilter2 is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			if (Compositor.IsSoftwareRenderer is true)
			{
				if (sourceFilter1 is not null)
				{
					return sourceFilter1;
				}
				else if (sourceFilter2 is not null)
				{
					return sourceFilter2;
				}
				else
				{
					return SKImageFilter.CreateOffset(0, 0, null, bounds);
				}
			}

			effectInterop.GetNamedPropertyMapping("CrossFade", out uint crossfadeProp, out _);

			float crossfade = (float)(effectInterop.GetProperty(crossfadeProp) ?? throw new InvalidOperationException("The effect property was null"));

			if (crossfade <= 0.0f)
			{
				return sourceFilter1;
			}
			else if (crossfade >= 1.0f)
			{
				return sourceFilter2;
			}

			SKImageFilter fbFilter = SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[]
				{
					crossfade, 0,         0,         0,         0,
					0,         crossfade, 0,         0,         0,
					0,         0,         crossfade, 0,         0,
					0,         0,         0,         crossfade, 0
				}), sourceFilter2);

			string shader =
"""
					uniform shader input;
					uniform half crossfade;

					half4 main() 
					{
						half4 inputColor = sample(input);
						return inputColor - (inputColor * crossfade);
					}
""";

			SKRuntimeEffect runtimeEffect = SKRuntimeEffect.CreateShader(shader, out string errors);
			if (errors is not null)
			{
				return null;
			}

			SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
			{
				{ "crossfade", crossfade }
			};

			SKRuntimeEffectChildren children = new(runtimeEffect);
			children.Add("input", null);

			SKImageFilter amafFilter = SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter1);

			return SKImageFilter.CreateBlendMode(SKBlendMode.Plus, fbFilter, amafFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateLuminanceToAlphaEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateLumaColor(), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateLinearTransferEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// Reference: https://learn.microsoft.com/en-us/windows/win32/direct2d/linear-transfer

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 13 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			if (Compositor.IsSoftwareRenderer is true)
			{
				if (sourceFilter is not null)
				{
					return sourceFilter;
				}
				else
				{
					return SKImageFilter.CreateOffset(0, 0, null, bounds);
				}
			}

			effectInterop.GetNamedPropertyMapping("RedOffset", out uint redOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("RedSlope", out uint redSlopeProp, out _);
			effectInterop.GetNamedPropertyMapping("RedDisable", out uint redDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("GreenOffset", out uint greenOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("GreenSlope", out uint greenSlopeProp, out _);
			effectInterop.GetNamedPropertyMapping("GreenDisable", out uint greenDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("BlueOffset", out uint blueOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("BlueSlope", out uint blueSlopeProp, out _);
			effectInterop.GetNamedPropertyMapping("BlueDisable", out uint blueDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("AlphaOffset", out uint alphaOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("AlphaSlope", out uint alphaSlopeProp, out _);
			effectInterop.GetNamedPropertyMapping("AlphaDisable", out uint alphaDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("ClampOutput", out uint clampProp, out _);

			float redOffset = (float)(effectInterop.GetProperty(redOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			float redSlope = (float)(effectInterop.GetProperty(redSlopeProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool redDisable = (bool)(effectInterop.GetProperty(redDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			float greenOffset = (float)(effectInterop.GetProperty(greenOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			float greenSlope = (float)(effectInterop.GetProperty(greenSlopeProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool greenDisable = (bool)(effectInterop.GetProperty(greenDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			float blueOffset = (float)(effectInterop.GetProperty(blueOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			float blueSlope = (float)(effectInterop.GetProperty(blueSlopeProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool blueDisable = (bool)(effectInterop.GetProperty(blueDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			float alphaOffset = (float)(effectInterop.GetProperty(alphaOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			float alphaSlope = (float)(effectInterop.GetProperty(alphaSlopeProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool alphaDisable = (bool)(effectInterop.GetProperty(alphaDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			bool clamp = clampProp != 0xFF ? (bool?)effectInterop.GetProperty(clampProp) ?? false : false;

			string shader =
$$"""
					uniform shader input;

					uniform half redOffset;
					uniform half redSlope;

					uniform half greenOffset;
					uniform half greenSlope;

					uniform half blueOffset;
					uniform half blueSlope;

					uniform half alphaOffset;
					uniform half alphaSlope;

					half4 Premultiply(half4 color)
					{
						color.rgb *= color.a;
						return color;
					}

					half4 UnPremultiply(half4 color)
					{
						color.rgb = (color.a == 0) ? half3(0, 0, 0) : (color.rgb / color.a);
						return color;
					}

					half4 main()
					{
						half4 color = UnPremultiply(sample(input));
						color = half4(
							{{(redDisable ? "color.r" : "redOffset + color.r * redSlope")}},
							{{(greenDisable ? "color.g" : "greenOffset + color.g * greenSlope")}},
							{{(blueDisable ? "color.b" : "blueOffset + color.b * blueSlope")}},
							{{(alphaDisable ? "color.a" : "alphaOffset + color.a * alphaSlope")}}
						);

						return {{(clamp ? "clamp(" : String.Empty)}}Premultiply(color){{(clamp ? ", 0.0, 1.0)" : String.Empty)}};
					}
""";

			SKRuntimeEffect runtimeEffect = SKRuntimeEffect.CreateShader(shader, out string errors);
			if (errors is not null)
			{
				return null;
			}

			SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
			{
				{ "redOffset", redOffset },
				{ "redSlope", redSlope },

				{ "greenOffset", greenOffset },
				{ "greenSlope", greenSlope },

				{ "blueOffset", blueOffset },
				{ "blueSlope", blueSlope },

				{ "alphaOffset", alphaOffset },
				{ "alphaSlope", alphaSlope }
			};

			SKRuntimeEffectChildren children = new(runtimeEffect);
			children.Add("input", null);

			return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateGammaTransferEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// Reference: https://learn.microsoft.com/en-us/windows/win32/direct2d/gamma-transfer

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 17 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			if (Compositor.IsSoftwareRenderer is true)
			{
				if (sourceFilter is not null)
				{
					return sourceFilter;
				}
				else
				{
					return SKImageFilter.CreateOffset(0, 0, null, bounds);
				}
			}

			effectInterop.GetNamedPropertyMapping("RedAmplitude", out uint redAmplitudeProp, out _);
			effectInterop.GetNamedPropertyMapping("RedExponent", out uint redExponentProp, out _);
			effectInterop.GetNamedPropertyMapping("RedOffset", out uint redOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("RedDisable", out uint redDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("GreenAmplitude", out uint greenAmplitudeProp, out _);
			effectInterop.GetNamedPropertyMapping("GreenExponent", out uint greenExponentProp, out _);
			effectInterop.GetNamedPropertyMapping("GreenOffset", out uint greenOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("GreenDisable", out uint greenDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("BlueAmplitude", out uint blueAmplitudeProp, out _);
			effectInterop.GetNamedPropertyMapping("BlueExponent", out uint blueExponentProp, out _);
			effectInterop.GetNamedPropertyMapping("BlueOffset", out uint blueOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("BlueDisable", out uint blueDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("AlphaAmplitude", out uint alphaAmplitudeProp, out _);
			effectInterop.GetNamedPropertyMapping("AlphaExponent", out uint alphaExponentProp, out _);
			effectInterop.GetNamedPropertyMapping("AlphaOffset", out uint alphaOffsetProp, out _);
			effectInterop.GetNamedPropertyMapping("AlphaDisable", out uint alphaDisableProp, out _);

			effectInterop.GetNamedPropertyMapping("ClampOutput", out uint clampProp, out _);

			float redAmplitude = (float)(effectInterop.GetProperty(redAmplitudeProp) ?? throw new InvalidOperationException("The effect property was null"));
			float redExponent = (float)(effectInterop.GetProperty(redExponentProp) ?? throw new InvalidOperationException("The effect property was null"));
			float redOffset = (float)(effectInterop.GetProperty(redOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool redDisable = (bool)(effectInterop.GetProperty(redDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			float greenAmplitude = (float)(effectInterop.GetProperty(greenAmplitudeProp) ?? throw new InvalidOperationException("The effect property was null"));
			float greenExponent = (float)(effectInterop.GetProperty(greenExponentProp) ?? throw new InvalidOperationException("The effect property was null"));
			float greenOffset = (float)(effectInterop.GetProperty(greenOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool greenDisable = (bool)(effectInterop.GetProperty(greenDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			float blueAmplitude = (float)(effectInterop.GetProperty(blueAmplitudeProp) ?? throw new InvalidOperationException("The effect property was null"));
			float blueExponent = (float)(effectInterop.GetProperty(blueExponentProp) ?? throw new InvalidOperationException("The effect property was null"));
			float blueOffset = (float)(effectInterop.GetProperty(blueOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool blueDisable = (bool)(effectInterop.GetProperty(blueDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			float alphaAmplitude = (float)(effectInterop.GetProperty(alphaAmplitudeProp) ?? throw new InvalidOperationException("The effect property was null"));
			float alphaExponent = (float)(effectInterop.GetProperty(alphaExponentProp) ?? throw new InvalidOperationException("The effect property was null"));
			float alphaOffset = (float)(effectInterop.GetProperty(alphaOffsetProp) ?? throw new InvalidOperationException("The effect property was null"));
			bool alphaDisable = (bool)(effectInterop.GetProperty(alphaDisableProp) ?? throw new InvalidOperationException("The effect property was null"));

			bool clamp = clampProp != 0xFF ? (bool?)effectInterop.GetProperty(clampProp) ?? false : false;

			string shader =
$$"""
					uniform shader input;

					uniform half redAmplitude;
					uniform half redExponent;
					uniform half redOffset;

					uniform half greenAmplitude;
					uniform half greenExponent;
					uniform half greenOffset;

					uniform half blueAmplitude;
					uniform half blueExponent;
					uniform half blueOffset;

					uniform half alphaAmplitude;
					uniform half alphaExponent;
					uniform half alphaOffset;

					half4 Premultiply(half4 color)
					{
						color.rgb *= color.a;
						return color;
					}

					half4 UnPremultiply(half4 color)
					{
						color.rgb = (color.a == 0) ? half3(0, 0, 0) : (color.rgb / color.a);
						return color;
					}

					half4 main()
					{
						half4 color = UnPremultiply(sample(input));
						color = half4(
							{{(redDisable ? "color.r" : "redAmplitude * pow(abs(color.r), redExponent) + redOffset")}},
							{{(greenDisable ? "color.g" : "greenAmplitude * pow(abs(color.g), greenExponent) + greenOffset")}},
							{{(blueDisable ? "color.b" : "blueAmplitude * pow(abs(color.b), blueExponent) + blueOffset")}},
							{{(alphaDisable ? "color.a" : "alphaAmplitude * pow(abs(color.a), alphaExponent) + alphaOffset")}}
						);

						return {{(clamp ? "clamp(" : String.Empty)}}Premultiply(color){{(clamp ? ", 0.0, 1.0)" : String.Empty)}};
					}
""";

			SKRuntimeEffect runtimeEffect = SKRuntimeEffect.CreateShader(shader, out string errors);
			if (errors is not null)
			{
				return null;
			}

			SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
			{
				{ "redAmplitude", redAmplitude },
				{ "redExponent", redExponent },
				{ "redOffset", redOffset },

				{ "greenAmplitude", greenAmplitude },
				{ "greenExponent", greenExponent },
				{ "greenOffset", greenOffset },

				{ "blueAmplitude", blueAmplitude },
				{ "blueExponent", blueExponent },
				{ "blueOffset", blueOffset },

				{ "alphaAmplitude", alphaAmplitude },
				{ "alphaExponent", alphaExponent },
				{ "alphaOffset", alphaOffset }
			};

			SKRuntimeEffectChildren children = new(runtimeEffect);
			children.Add("input", null);

			return SKImageFilter.CreateColorFilter(runtimeEffect.ToColorFilter(uniforms, children), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateTransform2DEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// TODO: Support "InterpolationMode", "BorderMode", and "Sharpness" properties

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 4 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("TransformMatrix", out uint matrixProp, out _);
			Matrix3x2? matrix = effectInterop.GetProperty(matrixProp) as Matrix3x2?;

			if (matrix is null)
			{
				float[]? matrixArray = effectInterop.GetProperty(matrixProp) as float[];

				if (matrixArray is not null && matrixArray.Length == 6)
				{
					matrix = new Matrix3x2(matrixArray[0], matrixArray[1], matrixArray[2], matrixArray[3], matrixArray[4], matrixArray[5]);
				}
				else
				{
					return null;
				}
			}

			return SKImageFilter.CreateMerge((ReadOnlySpan<SKImageFilter>)[SKImageFilter.CreateMatrix(matrix.Value.ToSKMatrix(), new SKSamplingOptions(SKCubicResampler.CatmullRom), sourceFilter)], bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateBorderEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// Note (1): Clamp and Mirror are currently broken, see https://github.com/mono/SkiaSharp/issues/866
		// Note (2): This currently only works correctly when the source is CompositionEffectSourceParameter, this is because of a limitation in SkiaSharp 2, we can support other source types one we upgrade to SkiaSharp 3

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 2 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds, tryUsingSourceSize: true);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("ExtendX", out uint modeXProp, out _);
			effectInterop.GetNamedPropertyMapping("ExtendY", out uint modeYProp, out _);
			D2D1BorderEdgeMode xmode = (D2D1BorderEdgeMode)(uint)(effectInterop.GetProperty(modeXProp) ?? throw new InvalidOperationException("The effect property was null"));
			D2D1BorderEdgeMode ymode = (D2D1BorderEdgeMode)(uint)(effectInterop.GetProperty(modeYProp) ?? throw new InvalidOperationException("The effect property was null"));

			SKShaderTileMode mode;

			//TODO: Support separate X,Y modes
			if (xmode != ymode)
			{
				if (xmode != default)
				{
					mode = xmode.ToSkia();
				}
				else
				{
					mode = ymode.ToSkia();
				}
			}
			else
			{
				mode = xmode.ToSkia();
			}

			if (mode == SKShaderTileMode.Repeat)
			{
				var srcBounds = source is CompositionEffectSourceParameter param && GetSourceParameter(param.Name) is ISizedBrush { Size: { } size }
					? new SKRect(0, 0, size.X, size.Y)
					: bounds;

				return SKImageFilter.CreateTile(srcBounds, bounds, sourceFilter);
			}
			else
			{
				ReadOnlySpan<float> identityKernel =
				[
					0,0,0,
					0,1,0,
					0,0,0
				];

				SKPointI kernelOffset = new(1, 1);
				return SKImageFilter.CreateMatrixConvolution(new SKSizeI(3, 3), identityKernel, 1f, 0f, kernelOffset, mode, true, sourceFilter, bounds);
			}
		}

		return null;
	}

	private SKImageFilter? GenerateSepiaEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// TODO: Support "AlphaMode" property maybe?

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Intensity", out uint intensityProp, out _);
			float intensity = (float)(effectInterop.GetProperty(intensityProp) ?? throw new InvalidOperationException("The effect property was null"));


			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // Sepia Matrix
				{
					0.393f + 0.607f * (1 - intensity), 0.769f - 0.769f * (1 - intensity), 0.189f - 0.189f * (1 - intensity), 0, 0,
					0.349f - 0.349f * (1 - intensity), 0.686f + 0.314f * (1 - intensity), 0.168f - 0.168f * (1 - intensity), 0, 0,
					0.272f - 0.272f * (1 - intensity), 0.534f - 0.534f * (1 - intensity), 0.131f + 0.869f * (1 - intensity), 0, 0,
					0,                                 0,                                 0,                                 1, 0
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateTemperatureAndTintEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 2 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Temperature", out uint tempProp, out _);
			effectInterop.GetNamedPropertyMapping("Tint", out uint tintProp, out _);

			float temp = (float)(effectInterop.GetProperty(tempProp) ?? throw new InvalidOperationException("The effect property was null"));
			float tint = (float)(effectInterop.GetProperty(tintProp) ?? throw new InvalidOperationException("The effect property was null"));

			var gains = TempAndTintHelpers.TempTintToGains(temp, tint);

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // TemperatureAndTint Matrix
				{
					gains.RedGain, 0,          0,              0, 0,
					0,             1,          0,              0, 0,
					0,             0,          gains.BlueGain, 0, 0,
					0,             0,          0,              1, 0,
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateColorMatrixEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// TODO: Support "AlphaMode" and "ClampOutput" properties

		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("ColorMatrix", out uint matrixProp, out _);
			float[] matrix = (float[])(effectInterop.GetProperty(matrixProp) ?? throw new InvalidOperationException("The effect property was null"));

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[]
				{
					matrix[0],  matrix[1],  matrix[2],  matrix[3],  matrix[16],
					matrix[4],  matrix[5],  matrix[6],  matrix[7],  matrix[17],
					matrix[8],  matrix[9],  matrix[10], matrix[11], matrix[18],
					matrix[12], matrix[13], matrix[14], matrix[15], matrix[19],
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateDistantDiffuseEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 4 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Azimuth", out uint azimuthProp, out GraphicsEffectPropertyMapping azimuthMapping);
			effectInterop.GetNamedPropertyMapping("Elevation", out uint elevationProp, out GraphicsEffectPropertyMapping elevationMapping);
			effectInterop.GetNamedPropertyMapping("DiffuseAmount", out uint amountProp, out _);
			effectInterop.GetNamedPropertyMapping("LightColor", out uint colorProp, out _);

			float azimuth = (float)(effectInterop.GetProperty(azimuthProp) ?? throw new InvalidOperationException("The effect property was null"));
			float elevation = (float)(effectInterop.GetProperty(elevationProp) ?? throw new InvalidOperationException("The effect property was null"));
			float amount = (float)(effectInterop.GetProperty(amountProp) ?? throw new InvalidOperationException("The effect property was null"));
			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));

			if (azimuthMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
			{
				azimuth *= 180.0f / MathF.PI;
			}

			if (elevationMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
			{
				elevation *= 180.0f / MathF.PI;
			}

			Vector3 lightVector = EffectHelpers.GetLightVector(azimuth, elevation);

			SKColor lightColor = color.ToSKColor();
			return SKImageFilter.CreateDistantLitDiffuse(new SKPoint3(lightVector.X, lightVector.Y, lightVector.Z), lightColor, 1.0f, amount, sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateDistantSpecularEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 5 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Azimuth", out uint azimuthProp, out GraphicsEffectPropertyMapping azimuthMapping);
			effectInterop.GetNamedPropertyMapping("Elevation", out uint elevationProp, out GraphicsEffectPropertyMapping elevationMapping);
			effectInterop.GetNamedPropertyMapping("SpecularExponent", out uint exponentProp, out _);
			effectInterop.GetNamedPropertyMapping("SpecularAmount", out uint amountProp, out _);
			effectInterop.GetNamedPropertyMapping("LightColor", out uint colorProp, out _);

			float azimuth = (float)(effectInterop.GetProperty(azimuthProp) ?? throw new InvalidOperationException("The effect property was null"));
			float elevation = (float)(effectInterop.GetProperty(elevationProp) ?? throw new InvalidOperationException("The effect property was null"));
			float exponent = (float)(effectInterop.GetProperty(exponentProp) ?? throw new InvalidOperationException("The effect property was null"));
			float amount = (float)(effectInterop.GetProperty(amountProp) ?? throw new InvalidOperationException("The effect property was null"));
			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));

			if (azimuthMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
			{
				azimuth *= 180.0f / MathF.PI;
			}

			if (elevationMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
			{
				elevation *= 180.0f / MathF.PI;
			}

			Vector3 lightVector = EffectHelpers.GetLightVector(azimuth, elevation);

			SKColor lightColor = color.ToSKColor();
			return SKImageFilter.CreateDistantLitSpecular(new SKPoint3(lightVector.X, lightVector.Y, lightVector.Z), lightColor, 1.0f, amount, exponent, sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateSpotDiffuseEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 6 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("LightPosition", out uint positionProp, out _);
			effectInterop.GetNamedPropertyMapping("LightTarget", out uint targetProp, out _);
			effectInterop.GetNamedPropertyMapping("Focus", out uint focusProp, out _);
			effectInterop.GetNamedPropertyMapping("LimitingConeAngle", out uint angleProp, out GraphicsEffectPropertyMapping angleMapping);
			effectInterop.GetNamedPropertyMapping("DiffuseAmount", out uint amountProp, out _);
			effectInterop.GetNamedPropertyMapping("LightColor", out uint colorProp, out _);

			Vector3 position = (Vector3)(effectInterop.GetProperty(positionProp) ?? throw new InvalidOperationException("The effect property was null"));
			Vector3 target = (Vector3)(effectInterop.GetProperty(targetProp) ?? throw new InvalidOperationException("The effect property was null"));
			float focus = (float)(effectInterop.GetProperty(focusProp) ?? throw new InvalidOperationException("The effect property was null"));
			float angle = (float)(effectInterop.GetProperty(angleProp) ?? throw new InvalidOperationException("The effect property was null"));
			float amount = (float)(effectInterop.GetProperty(amountProp) ?? throw new InvalidOperationException("The effect property was null"));
			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));

			if (angleMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
			{
				angle *= 180.0f / MathF.PI;
			}

			Vector3 lightTarget = EffectHelpers.CalculateLightTargetVector(position, target);

			return SKImageFilter.CreateSpotLitDiffuse(new SKPoint3(position.X, position.Y, position.Z), new SKPoint3(lightTarget.X, lightTarget.Y, lightTarget.Z), focus, angle, color.ToSKColor(), 1.0f, amount, sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateSpotSpecularEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 7 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("LightPosition", out uint positionProp, out _);
			effectInterop.GetNamedPropertyMapping("LightTarget", out uint targetProp, out _);
			effectInterop.GetNamedPropertyMapping("Focus", out uint focusProp, out _);
			effectInterop.GetNamedPropertyMapping("LimitingConeAngle", out uint angleProp, out GraphicsEffectPropertyMapping angleMapping);
			effectInterop.GetNamedPropertyMapping("SpecularExponent", out uint exponentProp, out _);
			effectInterop.GetNamedPropertyMapping("SpecularAmount", out uint amountProp, out _);
			effectInterop.GetNamedPropertyMapping("LightColor", out uint colorProp, out _);

			Vector3 position = (Vector3)(effectInterop.GetProperty(positionProp) ?? throw new InvalidOperationException("The effect property was null"));
			Vector3 target = (Vector3)(effectInterop.GetProperty(targetProp) ?? throw new InvalidOperationException("The effect property was null"));
			float focus = (float)(effectInterop.GetProperty(focusProp) ?? throw new InvalidOperationException("The effect property was null"));
			float angle = (float)(effectInterop.GetProperty(angleProp) ?? throw new InvalidOperationException("The effect property was null"));
			float exponent = (float)(effectInterop.GetProperty(exponentProp) ?? throw new InvalidOperationException("The effect property was null"));
			float amount = (float)(effectInterop.GetProperty(amountProp) ?? throw new InvalidOperationException("The effect property was null"));
			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));

			if (angleMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
			{
				angle *= 180.0f / MathF.PI;
			}

			Vector3 lightTarget = EffectHelpers.CalculateLightTargetVector(position, target);

			return SKImageFilter.CreateSpotLitSpecular(new SKPoint3(position.X, position.Y, position.Z), new SKPoint3(lightTarget.X, lightTarget.Y, lightTarget.Z), exponent, angle, color.ToSKColor(), 1.0f, amount, focus, sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GeneratePointDiffuseEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 3 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("LightPosition", out uint positionProp, out _);
			effectInterop.GetNamedPropertyMapping("DiffuseAmount", out uint amountProp, out _);
			effectInterop.GetNamedPropertyMapping("LightColor", out uint colorProp, out _);

			Vector3 position = (Vector3)(effectInterop.GetProperty(positionProp) ?? throw new InvalidOperationException("The effect property was null"));
			float amount = (float)(effectInterop.GetProperty(amountProp) ?? throw new InvalidOperationException("The effect property was null"));
			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));

			return SKImageFilter.CreatePointLitDiffuse(new SKPoint3(position.X, position.Y, position.Z), color.ToSKColor(), 1.0f, amount, sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GeneratePointSpecularEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() >= 4 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("LightPosition", out uint positionProp, out _);
			effectInterop.GetNamedPropertyMapping("SpecularExponent", out uint exponentProp, out _);
			effectInterop.GetNamedPropertyMapping("SpecularAmount", out uint amountProp, out _);
			effectInterop.GetNamedPropertyMapping("LightColor", out uint colorProp, out _);

			Vector3 position = (Vector3)(effectInterop.GetProperty(positionProp) ?? throw new InvalidOperationException("The effect property was null"));
			float exponent = (float)(effectInterop.GetProperty(exponentProp) ?? throw new InvalidOperationException("The effect property was null"));
			float amount = (float)(effectInterop.GetProperty(amountProp) ?? throw new InvalidOperationException("The effect property was null"));
			Color color = (Color)(effectInterop.GetProperty(colorProp) ?? throw new InvalidOperationException("The effect property was null"));

			return SKImageFilter.CreatePointLitSpecular(new SKPoint3(position.X, position.Y, position.Z), color.ToSKColor(), 1.0f, amount, exponent, sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateAlphaMaskEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// This is a temporary workaround until we move to Skia 3 so we can implement it properly using pixel shaders

		if (effectInterop.GetSourceCount() == 2 && effectInterop.GetSource(0) is IGraphicsEffectSource source && effectInterop.GetSource(1) is IGraphicsEffectSource mask)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			SKImageFilter? maskFilter = GenerateEffectFilter(mask, bounds);
			if (maskFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			return SKImageFilter.CreateBlendMode(SKBlendMode.SrcIn, maskFilter, sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateSaturationEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		if (effectInterop.GetSourceCount() == 1 && effectInterop.GetPropertyCount() == 1 && effectInterop.GetSource(0) is IGraphicsEffectSource source)
		{
			SKImageFilter? sourceFilter = GenerateEffectFilter(source, bounds);
			if (sourceFilter is null && !_isCurrentInputBackdrop)
			{
				return null;
			}

			_isCurrentInputBackdrop = false;

			effectInterop.GetNamedPropertyMapping("Saturation", out uint saturationProp, out _);

			float saturation = MathF.Min((float)(effectInterop.GetProperty(saturationProp) ?? throw new InvalidOperationException("The effect property was null")), 2);

			return SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
				new float[] // Saturation Matrix
				{
					0.2126f + 0.7874f * saturation, 0.7152f - 0.7152f * saturation, 0.0722f - 0.0722f * saturation, 0.0f, 0.0f,
					0.2126f - 0.2126f * saturation, 0.7152f + 0.2848f * saturation, 0.0722f - 0.0722f * saturation, 0.0f, 0.0f,
					0.2126f - 0.2126f * saturation, 0.7152f - 0.7152f * saturation, 0.0722f + 0.9278f * saturation, 0.0f, 0.0f,
					0.0f,                           0.0f,                           0.0f,                           1.0f, 0.0f
				}), sourceFilter, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateWhiteNoiseEffect(IGraphicsEffectD2D1Interop effectInterop, SKRect bounds)
	{
		// Reference: W. J. J. Rey. On generating random numbers, with help of y = [(a + x)sin(bx)] mod 1. presented at the 22nd European Meeting of Statisticians and the 7th Vilnius Conference on Probability Theory and Mathematical Statistics, August 1998, https://books.google.com/books/about/Probability_Theory_and_Mathematical_Stat.html?id=QTQk8tXrHKUC
		// Reference: https://www.shadertoy.com/view/4dS3Wd
		// Reference: https://thebookofshaders.com/10/

		if (effectInterop.GetPropertyCount() == 2)
		{
			if (Compositor.IsSoftwareRenderer is true)
			{
				return SKImageFilter.CreateOffset(0, 0, null, bounds);
			}

			effectInterop.GetNamedPropertyMapping("Frequency", out uint frequencyProp, out _);
			effectInterop.GetNamedPropertyMapping("Offset", out uint offsetProp, out _);

			Vector2 frequency = (Vector2)(effectInterop.GetProperty(frequencyProp) ?? throw new InvalidOperationException("The effect property was null"));
			Vector2 offset = (Vector2)(effectInterop.GetProperty(offsetProp) ?? throw new InvalidOperationException("The effect property was null"));

			string shader =
"""
					uniform half2 frequency;
					uniform half2 offset;

					half Hash(half2 p)
					{
						return fract(1e4 * sin(17.0 * p.x + p.y * 0.1) * (0.1 + abs(sin(p.y * 13.0 + p.x))));
					}


					half4 main(float2 coords) 
					{
						float2 coord = coords * 0.81 * frequency + offset;
						float2 px00 = floor(coord - 0.5) + 0.5;
						float2 px11 = px00 + 1;
						float2 px10 = float2(px11.x, px00.y);
						float2 px01 = float2(px00.x, px11.y);
						float2 factor = coord - px00;
						float sample00 = Hash(px00);
						float sample10 = Hash(px10);
						float sample01 = Hash(px01);
						float sample11 = Hash(px11);
						float result = mix(mix(sample00, sample10, factor.x), mix(sample01, sample11, factor.x), factor.y);

						return half4(result.xxx, 1);
					}
""";

			SKRuntimeEffect runtimeEffect = SKRuntimeEffect.CreateShader(shader, out string errors);
			if (errors is not null)
			{
				return null;
			}

			SKRuntimeEffectUniforms uniforms = new(runtimeEffect)
			{
				{ "frequency", new float[2] { frequency.X, frequency.Y } },
				{ "offset", new float[2] { offset.X, offset.Y } }
			};

			return SKImageFilter.CreateShader(runtimeEffect.ToShader(uniforms), false, bounds);
		}

		return null;
	}

	private SKImageFilter? GenerateEffectFilter(object? effect, SKRect bounds, bool tryUsingSourceSize = false)
	{
		// TODO: Cache pixel shaders, needed in order to implement animations and online rendering

		switch (effect)
		{
			case CompositionEffectSourceParameter effectSourceParameter:
				{
					CompositionBrush? brush = GetSourceParameter(effectSourceParameter.Name);
					if (brush is not null)
					{
						if (brush is CompositionBackdropBrush)
						{
							_isCurrentInputBackdrop = true;
							_hasBackdropBrushInputPrivate = true;
							return null;
						}

						_isCurrentInputBackdrop = false;

						SKRect srcBounds = bounds;
						if (tryUsingSourceSize && brush is ISizedBrush { Size: Vector2 size })
						{
							srcBounds = bounds with { Right = size.X, Bottom = size.Y };
						}

						// Creating a static SKPictureRecorder to be reused for all calls causes a segfault for some reason
						var recorder = new SKPictureRecorder();
						brush.Paint(recorder.BeginRecording(srcBounds), 1, srcBounds);
						return SKImageFilter.CreatePicture(recorder.EndRecording());
					}

					return null;
				}
			case IGraphicsEffectD2D1Interop effectInterop:
				{
					switch (EffectHelpers.GetEffectType(effectInterop.GetEffectId()))
					{
						case EffectType.GaussianBlurEffect:
							return GenerateGaussianBlurEffect(effectInterop, bounds);

						case EffectType.GrayscaleEffect:
							return GenerateGrayscaleEffect(effectInterop, bounds);

						case EffectType.InvertEffect:
							return GenerateInvertEffect(effectInterop, bounds);

						case EffectType.HueRotationEffect:
							return GenerateHueRotationEffect(effectInterop, bounds);

						case EffectType.TintEffect:
							return GenerateTintEffect(effectInterop, bounds);

						case EffectType.BlendEffect:
							return GenerateBlendEffect(effectInterop, bounds);

						case EffectType.CompositeEffect:
							return GenerateCompositeEffect(effectInterop, bounds);

						case EffectType.ColorSourceEffect:
							return GenerateColorSourceEffect(effectInterop, bounds);

						case EffectType.OpacityEffect:
							return GenerateOpacityEffect(effectInterop, bounds);

						case EffectType.ContrastEffect:
							return GenerateContrastEffect(effectInterop, bounds);

						case EffectType.ArithmeticCompositeEffect:
							return GenerateArithmeticCompositeEffect(effectInterop, bounds);

						case EffectType.ExposureEffect:
							return GenerateExposureEffect(effectInterop, bounds);

						case EffectType.CrossFadeEffect:
							return GenerateCrossFadeEffect(effectInterop, bounds);

						case EffectType.LuminanceToAlphaEffect:
							return GenerateLuminanceToAlphaEffect(effectInterop, bounds);

						case EffectType.LinearTransferEffect:
							return GenerateLinearTransferEffect(effectInterop, bounds);

						case EffectType.GammaTransferEffect:
							return GenerateGammaTransferEffect(effectInterop, bounds);

						case EffectType.Transform2DEffect:
							return GenerateTransform2DEffect(effectInterop, bounds);

						case EffectType.BorderEffect:
							return GenerateBorderEffect(effectInterop, bounds);

						case EffectType.SepiaEffect:
							return GenerateSepiaEffect(effectInterop, bounds);

						case EffectType.TemperatureAndTintEffect:
							return GenerateTemperatureAndTintEffect(effectInterop, bounds);

						case EffectType.ColorMatrixEffect:
							return GenerateColorMatrixEffect(effectInterop, bounds);

						case EffectType.DistantDiffuseEffect:
							return GenerateDistantDiffuseEffect(effectInterop, bounds);

						case EffectType.DistantSpecularEffect:
							return GenerateDistantSpecularEffect(effectInterop, bounds);

						case EffectType.SpotDiffuseEffect:
							return GenerateSpotDiffuseEffect(effectInterop, bounds);

						case EffectType.SpotSpecularEffect:
							return GenerateSpotSpecularEffect(effectInterop, bounds);

						case EffectType.PointDiffuseEffect:
							return GeneratePointDiffuseEffect(effectInterop, bounds);

						case EffectType.PointSpecularEffect:
							return GeneratePointSpecularEffect(effectInterop, bounds);

						case EffectType.AlphaMaskEffect:
							return GenerateAlphaMaskEffect(effectInterop, bounds);

						case EffectType.SaturationEffect:
							return GenerateSaturationEffect(effectInterop, bounds);

						case EffectType.WhiteNoiseEffect:
							return GenerateWhiteNoiseEffect(effectInterop, bounds);

						case EffectType.Unsupported:
						default:
							return null;
					}
				}
			default:
				return null;
		}
	}

	internal override void Paint(SKCanvas canvas, float opacity, SKRect bounds)
	{
		UpdateFilter(bounds);
		// Use the Bounds property to constrain the layer area and clip the canvas
		// to prevent nearby elements from bleeding through the acrylic effect.
		canvas.Save();
		canvas.ClipRect(bounds, antialias: true);
		canvas.SaveLayer(new SKCanvasSaveLayerRec { Backdrop = _filter, Bounds = bounds });
		canvas.Restore();
		canvas.Restore();
	}

	private void UpdateFilter(SKRect bounds)
	{
		if (_currentBounds != bounds || _filter is null || Compositor.IsSoftwareRenderer != _currentCompMode)
		{
			_isCurrentInputBackdrop = false;
			_hasBackdropBrushInputPrivate = false;
			_filter = GenerateEffectFilter(_effect, bounds) ?? throw new NotSupportedException($"Unsupported effect description.\r\nEffect name: {_effect.Name}");
			HasBackdropBrushInput = _hasBackdropBrushInputPrivate;
			_currentBounds = bounds;
			_currentCompMode = Compositor.IsSoftwareRenderer;
		}
	}

	private protected override void DisposeInternal()
	{
		base.DisposeInternal();

		_filter?.Dispose();
	}

	internal override bool CanPaint() => _effect is not null;
}
