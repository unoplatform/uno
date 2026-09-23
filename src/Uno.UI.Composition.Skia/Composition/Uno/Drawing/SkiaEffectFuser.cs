#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using SkiaSharp;
using Microsoft.UI.Composition;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Fuses a neutral <see cref="EffectNode"/> tree into a single <see cref="SKImageFilter"/> DAG — the Skia backend's
/// realization of <see cref="IDrawingFactory.CreateEffectFilter(EffectNode, Windows.Foundation.Rect)"/>. A
/// <see cref="SourceInput"/> fuses to a filter with a null (implicit) input, fed from the live backdrop via
/// <c>SaveLayerRec.Backdrop</c>. The source-flag handling matches <c>SkiaEffectFactory</c> for identical output.
/// </summary>
internal sealed class SkiaEffectFuser
{
	// Above this sigma an opted-in blur is downscaled first (the threshold master's acrylic used).
	private const float BlurDownscaleSigma = 8f;

	// Set when the current subtree resolved to the implicit source leaf (a null child filter that means "the
	// deferred source", not "failed to build"). A parent op keeps a null child in that case and clears the flag once consumed.
	private bool _isSource;

	// Every Skia wrapper a fuse allocates. Skia filters, colour filters and shaders are natively refcounted and a
	// parent takes its own reference on each input, so disposing all of these but the root frees exactly the orphans.
	private readonly List<SKObject> _intermediates = new();

	/// <summary>Why the last <see cref="FuseGraph"/> produced no filter; null when it produced one.</summary>
	internal string? FailureReason { get; private set; }

	/// <summary>
	/// Fuses <paramref name="node"/> into a single filter and releases every intermediate the fuse allocated — only
	/// the returned root reaches the caller. Null means the graph can't be realized (see <see cref="FailureReason"/>).
	/// </summary>
	internal SKImageFilter? FuseGraph(EffectNode node, SKRect bounds)
	{
		SKImageFilter? root = null;
		try
		{
			root = Fuse(node, bounds);
			if (root is null)
			{
				FailureReason ??= _isSource
					? "the graph is a bare backdrop source with no operation applied"
					: $"'{node.GetType().Name}' produced no filter";
			}

			return root;
		}
		finally
		{
			foreach (var intermediate in _intermediates)
			{
				if (!ReferenceEquals(intermediate, root))
				{
					intermediate.Dispose();
				}
			}

			_intermediates.Clear();
		}
	}

	// Skia's factories return null on failure, so the tracked resource is nullable in and nullable out.
	[return: NotNullIfNotNull(nameof(resource))]
	private T? Track<T>(T? resource) where T : SKObject
	{
		if (resource is not null)
		{
			_intermediates.Add(resource);
		}

		return resource;
	}

	private SKImageFilter? Fail(string reason)
	{
		FailureReason ??= reason;
		return null;
	}

	private SKImageFilter? Fuse(EffectNode node, SKRect bounds)
	{
		switch (node)
		{
			case SourceInput:
				_isSource = true;
				return null;

			case TextureInput texture:
				{
					_isSource = false;
					var img = ((SkiaTexture)texture.Texture).Image;
					var src = new SKRect(0, 0, img.Width, img.Height);

					if (texture.ExtendX == EdgeExtend.None && texture.ExtendY == EdgeExtend.None)
					{
						// Plain finite image: place it back at bounds (it was rasterized in bounds-space at the origin).
						var dst = new SKRect(bounds.Left, bounds.Top, bounds.Left + img.Width, bounds.Top + img.Height);
						return Track(SKImageFilter.CreateImage(img, src, dst, new SKSamplingOptions(SKFilterMode.Linear)));
					}

					// BorderEffect: extend the source's own rectangle to infinity per the edge mode; downstream sampling
					// over `bounds` then sees the tiled/mirrored/clamped fill. Mirrors the legacy Border realization.
					var imageFilter = Track(SKImageFilter.CreateImage(img, src, src, new SKSamplingOptions(SKFilterMode.Linear)));
					var mode = PickExtend(texture.ExtendX, texture.ExtendY);
					if (mode == SKShaderTileMode.Repeat)
					{
						return Track(SKImageFilter.CreateTile(src, bounds, imageFilter));
					}

					ReadOnlySpan<float> identityKernel = [0, 0, 0, 0, 1, 0, 0, 0, 0];
					return Track(SKImageFilter.CreateMatrixConvolution(new SKSizeI(3, 3), identityKernel, 1f, 0f, new SKPointI(1, 1), mode, true, imageFilter, bounds));
				}

			case ColorInput color:
				// ColorSource fills bounds; no input. Does not clear the backdrop flag (matches the legacy generator).
				return Track(SKImageFilter.CreateColorFilter(Track(SKColorFilter.CreateBlendMode(color.Color.ToSKColor(), SKBlendMode.Src)), null, bounds));

			case ColorMatrixEffectNode cm:
				{
					var source = Fuse(cm.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					return Track(SKImageFilter.CreateColorFilter(Track(SKColorFilter.CreateColorMatrix(cm.Matrix)), source, bounds));
				}

			case ModulateEffectNode modulate:
				{
					var source = Fuse(modulate.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					// Tint: per-channel multiply by the colour, clamped to [0,1] — matches the legacy Tint realization.
					return Track(SKImageFilter.CreateColorFilter(Track(SKColorFilter.CreateBlendMode(modulate.Color.ToSKColor(), SKBlendMode.Modulate)), source, bounds));
				}

			case LuminanceToAlphaEffectNode luma:
				{
					var source = Fuse(luma.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					return Track(SKImageFilter.CreateColorFilter(Track(SKColorFilter.CreateLumaColor()), source, bounds));
				}

			case ContrastEffectNode contrast:
				{
					var source = Fuse(contrast.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					var clamp = contrast.Clamp;
					var shader =
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
		return Contrast({{(clamp ? "clamp(" : string.Empty)}}sample(input){{(clamp ? ", 0.0, 1.0)" : string.Empty)}}, contrastValue);
	}
""";

					var runtimeEffect = SKRuntimeEffect.CreateShader(shader, out var errors);
					if (errors is not null)
					{
						return Fail($"an effect shader failed to compile: {errors}");
					}

					Track(runtimeEffect);
					var uniforms = new SKRuntimeEffectUniforms(runtimeEffect) { { "contrastValue", contrast.Contrast } };
					var children = new SKRuntimeEffectChildren(runtimeEffect);
					children.Add("input", null);
					return Track(SKImageFilter.CreateColorFilter(Track(runtimeEffect.ToColorFilter(uniforms, children)), source, bounds));
				}

			case LinearTransferEffectNode transfer:
				{
					var source = Fuse(transfer.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					var clamp = transfer.Clamp;
					var shader =
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
			{{(transfer.Disable[0] ? "color.r" : "redOffset + color.r * redSlope")}},
			{{(transfer.Disable[1] ? "color.g" : "greenOffset + color.g * greenSlope")}},
			{{(transfer.Disable[2] ? "color.b" : "blueOffset + color.b * blueSlope")}},
			{{(transfer.Disable[3] ? "color.a" : "alphaOffset + color.a * alphaSlope")}}
		);

		return {{(clamp ? "clamp(" : string.Empty)}}Premultiply(color){{(clamp ? ", 0.0, 1.0)" : string.Empty)}};
	}
""";

					var runtimeEffect = SKRuntimeEffect.CreateShader(shader, out var errors);
					if (errors is not null)
					{
						return Fail($"an effect shader failed to compile: {errors}");
					}

					Track(runtimeEffect);
					var uniforms = new SKRuntimeEffectUniforms(runtimeEffect)
				{
					{ "redOffset", transfer.Offsets[0] },
					{ "redSlope", transfer.Slopes[0] },
					{ "greenOffset", transfer.Offsets[1] },
					{ "greenSlope", transfer.Slopes[1] },
					{ "blueOffset", transfer.Offsets[2] },
					{ "blueSlope", transfer.Slopes[2] },
					{ "alphaOffset", transfer.Offsets[3] },
					{ "alphaSlope", transfer.Slopes[3] },
				};
					var children = new SKRuntimeEffectChildren(runtimeEffect);
					children.Add("input", null);
					return Track(SKImageFilter.CreateColorFilter(Track(runtimeEffect.ToColorFilter(uniforms, children)), source, bounds));
				}

			case GammaTransferEffectNode gamma:
				{
					var source = Fuse(gamma.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					var clamp = gamma.Clamp;
					var shader =
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
			{{(gamma.Disable[0] ? "color.r" : "redAmplitude * pow(abs(color.r), redExponent) + redOffset")}},
			{{(gamma.Disable[1] ? "color.g" : "greenAmplitude * pow(abs(color.g), greenExponent) + greenOffset")}},
			{{(gamma.Disable[2] ? "color.b" : "blueAmplitude * pow(abs(color.b), blueExponent) + blueOffset")}},
			{{(gamma.Disable[3] ? "color.a" : "alphaAmplitude * pow(abs(color.a), alphaExponent) + alphaOffset")}}
		);

		return {{(clamp ? "clamp(" : string.Empty)}}Premultiply(color){{(clamp ? ", 0.0, 1.0)" : string.Empty)}};
	}
""";

					var runtimeEffect = SKRuntimeEffect.CreateShader(shader, out var errors);
					if (errors is not null)
					{
						return Fail($"an effect shader failed to compile: {errors}");
					}

					Track(runtimeEffect);
					var uniforms = new SKRuntimeEffectUniforms(runtimeEffect)
				{
					{ "redAmplitude", gamma.Amplitudes[0] },
					{ "redExponent", gamma.Exponents[0] },
					{ "redOffset", gamma.Offsets[0] },
					{ "greenAmplitude", gamma.Amplitudes[1] },
					{ "greenExponent", gamma.Exponents[1] },
					{ "greenOffset", gamma.Offsets[1] },
					{ "blueAmplitude", gamma.Amplitudes[2] },
					{ "blueExponent", gamma.Exponents[2] },
					{ "blueOffset", gamma.Offsets[2] },
					{ "alphaAmplitude", gamma.Amplitudes[3] },
					{ "alphaExponent", gamma.Exponents[3] },
					{ "alphaOffset", gamma.Offsets[3] },
				};
					var children = new SKRuntimeEffectChildren(runtimeEffect);
					children.Add("input", null);
					return Track(SKImageFilter.CreateColorFilter(Track(runtimeEffect.ToColorFilter(uniforms, children)), source, bounds));
				}

			case Transform2DEffectNode transform:
				{
					var source = Fuse(transform.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					return Track(SKImageFilter.CreateMerge(
						(ReadOnlySpan<SKImageFilter>)[Track(SKImageFilter.CreateMatrix(transform.Matrix.ToSKMatrix(), new SKSamplingOptions(SKCubicResampler.CatmullRom), source))],
						bounds));
				}

			case BlurEffectNode blur:
				{
					var source = Fuse(blur.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;

					// An opted-in wide blur runs at reduced resolution: cost falls with the square of the factor and
					// the result is read blurred anyway. The crop is the blur's own reach, not the tight bounds --
					// cropping at the element edge darkens it, which is what the backdrop tests catch.
					var scale = blur.Downscale ? Math.Max(1, (int)(blur.Sigma / BlurDownscaleSigma)) : 1;
					if (scale > 1)
					{
						var inv = 1f / scale;
						var sampling = new SKSamplingOptions(SKFilterMode.Linear);
						var downscaled = Track(SKImageFilter.CreateMatrix(SKMatrix.CreateScale(inv, inv), sampling, source));
						var reduced = Track(blur.ClampEdge
							? SKImageFilter.CreateBlur(blur.Sigma * inv, blur.Sigma * inv, SKShaderTileMode.Clamp, downscaled)
							: SKImageFilter.CreateBlur(blur.Sigma * inv, blur.Sigma * inv, downscaled));
						var upscaled = Track(SKImageFilter.CreateMatrix(SKMatrix.CreateScale(scale, scale), sampling, reduced));
						var reach = 3f * blur.Sigma;
						var blurBounds = SKRect.Create(
							bounds.Left - reach, bounds.Top - reach,
							bounds.Width + (2f * reach), bounds.Height + (2f * reach));
						return Track(SKImageFilter.CreateMerge((ReadOnlySpan<SKImageFilter>)[upscaled], blurBounds));
					}

					return Track(blur.ClampEdge
						? SKImageFilter.CreateBlur(blur.Sigma, blur.Sigma, SKShaderTileMode.Clamp, source, bounds)
						: SKImageFilter.CreateBlur(blur.Sigma, blur.Sigma, source, bounds));
				}

			case BlendEffectNode blend:
				{
					var background = Fuse(blend.Background, bounds);
					if (background is null && !_isSource)
					{
						return null;
					}

					var foreground = Fuse(blend.Foreground, bounds);
					if (foreground is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					return Track(SKImageFilter.CreateBlendMode(SkiaDrawingSession.ToSKBlendMode(blend.Mode), background, foreground, bounds));
				}

			case CompositeEffectNode composite:
				{
					if (composite.Sources.Count == 0)
					{
						return Fail("the composite effect has no sources");
					}

					var current = Fuse(composite.Sources[0], bounds);
					if (current is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					var mode = SkiaDrawingSession.ToSKBlendMode(composite.Mode);
					for (var i = 1; i < composite.Sources.Count; i++)
					{
						var next = Fuse(composite.Sources[i], bounds);
						if (next is not null && !_isSource)
						{
							current = Track(SKImageFilter.CreateBlendMode(mode, current, next, bounds));
						}

						_isSource = false;
					}

					return current;
				}

			case AlphaMaskEffectNode alphaMask:
				{
					var sourceFilter = Fuse(alphaMask.Source, bounds);
					if (sourceFilter is null && !_isSource)
					{
						return null;
					}

					var maskFilter = Fuse(alphaMask.Mask, bounds);
					if (maskFilter is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					return Track(SKImageFilter.CreateBlendMode(SKBlendMode.SrcIn, maskFilter, sourceFilter, bounds));
				}

			case ArithmeticCompositeEffectNode arithmetic:
				{
					var background = Fuse(arithmetic.Background, bounds);
					if (background is null && !_isSource)
					{
						return null;
					}

					var foreground = Fuse(arithmetic.Foreground, bounds);
					if (foreground is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					return Track(SKImageFilter.CreateArithmetic(arithmetic.Multiply, arithmetic.Source1, arithmetic.Source2, arithmetic.Offset, false, background, foreground, bounds));
				}

			case CrossFadeEffectNode crossFade:
				{
					var filter1 = Fuse(crossFade.SourceB, bounds);
					if (filter1 is null && !_isSource)
					{
						return null;
					}

					var filter2 = Fuse(crossFade.SourceA, bounds);
					if (filter2 is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					var weight = crossFade.Weight;
					if (weight <= 0.0f)
					{
						return filter1;
					}

					if (weight >= 1.0f)
					{
						return filter2;
					}

					var fbFilter = Track(SKImageFilter.CreateColorFilter(Track(SKColorFilter.CreateColorMatrix(
						new[]
						{
						weight, 0f,     0f,     0f,     0f,
						0f,     weight, 0f,     0f,     0f,
						0f,     0f,     weight, 0f,     0f,
						0f,     0f,     0f,     weight, 0f,
						})), filter2));

					var shader =
	"""
	uniform shader input;
	uniform half crossfade;

	half4 main()
	{
		half4 inputColor = sample(input);
		return inputColor - (inputColor * crossfade);
	}
""";

					var crossFadeEffect = SKRuntimeEffect.CreateShader(shader, out var crossFadeErrors);
					if (crossFadeErrors is not null)
					{
						return Fail($"the cross-fade shader failed to compile: {crossFadeErrors}");
					}

					Track(crossFadeEffect);
					var crossFadeUniforms = new SKRuntimeEffectUniforms(crossFadeEffect) { { "crossfade", weight } };
					var crossFadeChildren = new SKRuntimeEffectChildren(crossFadeEffect);
					crossFadeChildren.Add("input", null);
					var amafFilter = Track(SKImageFilter.CreateColorFilter(Track(crossFadeEffect.ToColorFilter(crossFadeUniforms, crossFadeChildren)), filter1));
					return Track(SKImageFilter.CreateBlendMode(SKBlendMode.Plus, fbFilter, amafFilter, bounds));
				}

			case WhiteNoiseEffectNode noise:
				{
					var shader =
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

					var noiseEffect = SKRuntimeEffect.CreateShader(shader, out var noiseErrors);
					if (noiseErrors is not null)
					{
						return Fail($"the noise shader failed to compile: {noiseErrors}");
					}

					Track(noiseEffect);
					var noiseUniforms = new SKRuntimeEffectUniforms(noiseEffect)
				{
					{ "frequency", new[] { noise.Frequency.X, noise.Frequency.Y } },
					{ "offset", new[] { noise.Offset.X, noise.Offset.Y } },
				};
					return Track(SKImageFilter.CreateShader(Track(noiseEffect.ToShader(noiseUniforms)), false, bounds));
				}

			case LightingEffectNode lighting:
				{
					var source = Fuse(lighting.Source, bounds);
					if (source is null && !_isSource)
					{
						return null;
					}

					_isSource = false;
					var light = new SKPoint3(lighting.Light.X, lighting.Light.Y, lighting.Light.Z);
					var target = new SKPoint3(lighting.Target.X, lighting.Target.Y, lighting.Target.Z);
					var color = lighting.LightColor.ToSKColor();
					var lit = lighting.Kind switch
					{
						LightingKind.DistantDiffuse => SKImageFilter.CreateDistantLitDiffuse(light, color, 1f, lighting.Amount, source, bounds),
						LightingKind.DistantSpecular => SKImageFilter.CreateDistantLitSpecular(light, color, 1f, lighting.Amount, lighting.SpecularExponent, source, bounds),
						LightingKind.SpotDiffuse => SKImageFilter.CreateSpotLitDiffuse(light, target, lighting.Focus, lighting.ConeAngle, color, 1f, lighting.Amount, source, bounds),
						LightingKind.SpotSpecular => SKImageFilter.CreateSpotLitSpecular(light, target, lighting.SpecularExponent, lighting.ConeAngle, color, 1f, lighting.Amount, lighting.Focus, source, bounds),
						LightingKind.PointDiffuse => SKImageFilter.CreatePointLitDiffuse(light, color, 1f, lighting.Amount, source, bounds),
						LightingKind.PointSpecular => SKImageFilter.CreatePointLitSpecular(light, color, 1f, lighting.Amount, lighting.SpecularExponent, source, bounds),
						_ => null,
					};

					return lit is null ? Fail($"unsupported lighting kind '{lighting.Kind}'") : Track(lit);
				}

			case UnsupportedEffectNode unsupported:
				return unsupported.Source is null ? Fail($"unsupported effect '{unsupported.EffectName}'") : Fuse(unsupported.Source, bounds);

			default:
				return Fail($"no Skia realization for '{node.GetType().Name}'");
		}
	}

	// Combines the two axis extend modes into one Skia tile mode, mirroring the legacy Border's "prefer the
	// non-Clamp axis" behaviour (SkiaSharp can't yet apply independent X/Y modes).
	private static SKShaderTileMode PickExtend(EdgeExtend x, EdgeExtend y)
	{
		var pick = x != y ? (x != EdgeExtend.Clamp ? x : y) : x;
		return pick switch
		{
			EdgeExtend.Wrap => SKShaderTileMode.Repeat,
			EdgeExtend.Mirror => SKShaderTileMode.Mirror,
			_ => SKShaderTileMode.Clamp,
		};
	}
}
