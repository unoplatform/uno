#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Microsoft.UI.Composition;
using Uno.Foundation.Logging;
using Uno.UI.Composition;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Effects;

/// <summary>
/// Parses a WinUI effect graph into the neutral <see cref="EffectNode"/> IR, resolving named source-parameters to
/// <see cref="IEffectSource"/>. <b>All</b> Direct2D reflection — effect GUIDs, property name→index mapping, boxed
/// property values, the recursive source walk — lives here, once. A render backend then <b>fuses</b> the typed tree
/// and never interprets an <c>IGraphicsEffect</c>. Every non-backdrop brush input is pre-rasterized to a
/// <see cref="TextureInput"/> here (via <see cref="IDrawingFactory.RenderOffscreen"/>), so the backend sees only a
/// texture. See <c>specs/effects-neutralization/design.md</c>.
/// </summary>
internal static class EffectGraphParser
{
	/// <summary>
	/// Parses <paramref name="effect"/> (a graph node or a source-parameter leaf) into an <see cref="EffectNode"/>
	/// tree bounded by <paramref name="bounds"/> (the region non-backdrop sources are rasterized over).
	/// </summary>
	public static EffectNode Parse(object? effect, Rect bounds, Func<string, IEffectSource?> resolveSource)
	{
		switch (effect)
		{
			case CompositionEffectSourceParameter sourceParameter:
			{
				var source = resolveSource(sourceParameter.Name);
				if (source is null)
				{
					return new UnsupportedEffectNode($"unbound source '{sourceParameter.Name}'", null);
				}

				if (source.IsBackdrop)
				{
					return new BackdropInput();
				}

				// Non-backdrop brush/image/noise input: rasterize it to a backend texture once, so the backend never
				// paints a compositor brush. The texture is placed back at the source's bounds by the fuser.
				return RasterizeSource(source, bounds);
			}

			case IGraphicsEffectD2D1Interop interop:
				return ParseNode(interop, bounds, resolveSource);

			default:
				return new UnsupportedEffectNode(effect?.GetType().Name ?? "null", null);
		}
	}

	private static TextureInput RasterizeSource(IEffectSource source, Rect bounds, Vector2? intrinsicSize = null, EdgeExtend extendX = EdgeExtend.None, EdgeExtend extendY = EdgeExtend.None)
	{
		// A Border input is rasterized at its own intrinsic size (the repeating/extend unit); everything else is
		// rasterized over the effect bounds.
		var region = intrinsicSize is { } size ? new Rect(0, 0, size.X, size.Y) : bounds;
		var width = Math.Max(1, (int)Math.Ceiling(region.Width));
		var height = Math.Max(1, (int)Math.Ceiling(region.Height));

		var texture = DrawingFactory.Current.RenderOffscreen(width, height, session =>
		{
			// The source paints in region-space; translate so the region's origin maps to the offscreen origin.
			if (region.X != 0 || region.Y != 0)
			{
				session.Translate(-(float)region.X, -(float)region.Y);
			}

			source.Paint(session, 1f, region);
		});

		return new TextureInput(texture, extendX, extendY);
	}

	private static EffectNode ParseNode(IGraphicsEffectD2D1Interop e, Rect bounds, Func<string, IEffectSource?> resolveSource)
	{
		EffectNode Src(uint i) => Parse(e.GetSource(i), bounds, resolveSource);
		object Prop(string name)
		{
			e.GetNamedPropertyMapping(name, out var index, out _);
			return e.GetProperty(index) ?? throw new InvalidOperationException($"Effect property '{name}' was null.");
		}
		(object Value, GraphicsEffectPropertyMapping Mapping) PropM(string name)
		{
			e.GetNamedPropertyMapping(name, out var index, out var mapping);
			return (e.GetProperty(index) ?? throw new InvalidOperationException($"Effect property '{name}' was null."), mapping);
		}
		// Optional bool property (0xFF index == absent), defaulting false — mirrors the legacy ClampSource/ClampOutput reads.
		bool PropBool(string name)
		{
			e.GetNamedPropertyMapping(name, out var index, out _);
			return index != 0xFF && e.GetProperty(index) is bool value && value;
		}
		// An angle property, converted radians→degrees when the effect declares that mapping (lighting helpers want degrees).
		float Angle(string name)
		{
			var (value, mapping) = PropM(name);
			var angle = (float)value;
			return mapping == GraphicsEffectPropertyMapping.RadiansToDegrees ? angle * 180f / MathF.PI : angle;
		}

		var type = EffectHelpers.GetEffectType(e.GetEffectId());
		switch (type)
		{
			case EffectType.GaussianBlurEffect:
			{
				var sigma = (float)Prop("BlurAmount");
				var hardBorder = Prop("BorderMode") is uint borderMode && borderMode == 1; // EffectBorderMode.Hard
				return new BlurEffectNode(Src(0), sigma, hardBorder);
			}

			case EffectType.ColorSourceEffect:
				return new ColorInput((Color)Prop("Color"));

			case EffectType.OpacityEffect:
			{
				var opacity = (float)Prop("Opacity");
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					1f, 0f, 0f, 0f,      0f,
					0f, 1f, 0f, 0f,      0f,
					0f, 0f, 1f, 0f,      0f,
					0f, 0f, 0f, opacity, 0f,
				});
			}

			case EffectType.ColorMatrixEffect:
			{
				// D2D ColorMatrix is a 5×4 (column-major) float[20]; re-order to the 4×5 row-major layout the neutral
				// colour-matrix node uses (rows R,G,B,A; last column = bias). Matches the current Skia realization.
				var m = (float[])Prop("ColorMatrix");
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					m[0],  m[1],  m[2],  m[3],  m[16],
					m[4],  m[5],  m[6],  m[7],  m[17],
					m[8],  m[9],  m[10], m[11], m[18],
					m[12], m[13], m[14], m[15], m[19],
				});
			}

			case EffectType.BlendEffect:
				return new BlendEffectNode(Src(0), Src(1), MapBlend((D2D1BlendEffectMode)(uint)Prop("Mode")));

			case EffectType.CompositeEffect:
			{
				var mode = MapComposite((D2D1CompositeMode)(uint)Prop("Mode"));
				var count = e.GetSourceCount();
				var sources = new List<EffectNode>((int)count);
				for (uint i = 0; i < count; i++)
				{
					sources.Add(Src(i));
				}

				return new CompositeEffectNode(sources, mode);
			}

			case EffectType.GrayscaleEffect:
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					0.21f, 0.72f, 0.07f, 0f, 0f,
					0.21f, 0.72f, 0.07f, 0f, 0f,
					0.21f, 0.72f, 0.07f, 0f, 0f,
					0f,    0f,    0f,    1f, 0f,
				});

			case EffectType.InvertEffect:
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					-1f, 0f,  0f,  0f, 1f,
					0f,  -1f, 0f,  0f, 1f,
					0f,  0f,  -1f, 0f, 1f,
					0f,  0f,  0f,  1f, 0f,
				});

			case EffectType.HueRotationEffect:
			{
				var (angleValue, angleMapping) = PropM("Angle");
				var angle = (float)angleValue;
				if (angleMapping == GraphicsEffectPropertyMapping.RadiansToDegrees)
				{
					angle *= 180.0f / MathF.PI;
				}

				return new ColorMatrixEffectNode(Src(0), new[]
				{
					0.2127f + MathF.Cos(angle) * 0.7873f - MathF.Sin(angle) * 0.2127f, 0.715f - MathF.Cos(angle) * 0.715f - MathF.Sin(angle) * 0.715f, 0.072f - MathF.Cos(angle) * 0.072f + MathF.Sin(angle) * 0.928f, 0f, 0f,
					0.2127f - MathF.Cos(angle) * 0.213f + MathF.Sin(angle) * 0.143f,   0.715f + MathF.Cos(angle) * 0.285f + MathF.Sin(angle) * 0.140f, 0.072f - MathF.Cos(angle) * 0.072f - MathF.Sin(angle) * 0.283f, 0f, 0f,
					0.2127f - MathF.Cos(angle) * 0.213f - MathF.Sin(angle) * 0.787f,   0.715f - MathF.Cos(angle) * 0.715f + MathF.Sin(angle) * 0.715f, 0.072f + MathF.Cos(angle) * 0.928f + MathF.Sin(angle) * 0.072f, 0f, 0f,
					0f,                                                                0f,                                                            0f,                                                            1f, 0f,
				});
			}

			case EffectType.ExposureEffect:
			{
				var multiplier = MathF.Pow(2.0f, (float)Prop("Exposure"));
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					multiplier, 0f,         0f,         0f, 0f,
					0f,         multiplier, 0f,         0f, 0f,
					0f,         0f,         multiplier, 0f, 0f,
					0f,         0f,         0f,         1f, 0f,
				});
			}

			case EffectType.SepiaEffect:
			{
				var intensity = (float)Prop("Intensity");
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					0.393f + 0.607f * (1 - intensity), 0.769f - 0.769f * (1 - intensity), 0.189f - 0.189f * (1 - intensity), 0f, 0f,
					0.349f - 0.349f * (1 - intensity), 0.686f + 0.314f * (1 - intensity), 0.168f - 0.168f * (1 - intensity), 0f, 0f,
					0.272f - 0.272f * (1 - intensity), 0.534f - 0.534f * (1 - intensity), 0.131f + 0.869f * (1 - intensity), 0f, 0f,
					0f,                                0f,                                0f,                                1f, 0f,
				});
			}

			case EffectType.TemperatureAndTintEffect:
			{
				var gains = TempAndTintHelpers.TempTintToGains((float)Prop("Temperature"), (float)Prop("Tint"));
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					gains.RedGain, 0f, 0f,             0f, 0f,
					0f,            1f, 0f,             0f, 0f,
					0f,            0f, gains.BlueGain, 0f, 0f,
					0f,            0f, 0f,             1f, 0f,
				});
			}

			case EffectType.SaturationEffect:
			{
				var saturation = MathF.Min((float)Prop("Saturation"), 2);
				return new ColorMatrixEffectNode(Src(0), new[]
				{
					0.2126f + 0.7874f * saturation, 0.7152f - 0.7152f * saturation, 0.0722f - 0.0722f * saturation, 0f, 0f,
					0.2126f - 0.2126f * saturation, 0.7152f + 0.2848f * saturation, 0.0722f - 0.0722f * saturation, 0f, 0f,
					0.2126f - 0.2126f * saturation, 0.7152f - 0.7152f * saturation, 0.0722f + 0.9278f * saturation, 0f, 0f,
					0f,                             0f,                             0f,                             1f, 0f,
				});
			}

			case EffectType.TintEffect:
				return new ModulateEffectNode(Src(0), (Color)Prop("Color"));

			case EffectType.LuminanceToAlphaEffect:
				return new LuminanceToAlphaEffectNode(Src(0));

			case EffectType.ContrastEffect:
				return new ContrastEffectNode(Src(0), (float)Prop("Contrast"), PropBool("ClampSource"));

			case EffectType.LinearTransferEffect:
				return new LinearTransferEffectNode(
					Src(0),
					new[] { (float)Prop("RedOffset"), (float)Prop("GreenOffset"), (float)Prop("BlueOffset"), (float)Prop("AlphaOffset") },
					new[] { (float)Prop("RedSlope"), (float)Prop("GreenSlope"), (float)Prop("BlueSlope"), (float)Prop("AlphaSlope") },
					new[] { (bool)Prop("RedDisable"), (bool)Prop("GreenDisable"), (bool)Prop("BlueDisable"), (bool)Prop("AlphaDisable") },
					PropBool("ClampOutput"));

			case EffectType.GammaTransferEffect:
				return new GammaTransferEffectNode(
					Src(0),
					new[] { (float)Prop("RedAmplitude"), (float)Prop("GreenAmplitude"), (float)Prop("BlueAmplitude"), (float)Prop("AlphaAmplitude") },
					new[] { (float)Prop("RedExponent"), (float)Prop("GreenExponent"), (float)Prop("BlueExponent"), (float)Prop("AlphaExponent") },
					new[] { (float)Prop("RedOffset"), (float)Prop("GreenOffset"), (float)Prop("BlueOffset"), (float)Prop("AlphaOffset") },
					new[] { (bool)Prop("RedDisable"), (bool)Prop("GreenDisable"), (bool)Prop("BlueDisable"), (bool)Prop("AlphaDisable") },
					PropBool("ClampOutput"));

			case EffectType.Transform2DEffect:
			{
				var raw = Prop("TransformMatrix");
				var matrix = raw is Matrix3x2 m3
					? m3
					: raw is float[] a && a.Length == 6 ? new Matrix3x2(a[0], a[1], a[2], a[3], a[4], a[5]) : Matrix3x2.Identity;
				return new Transform2DEffectNode(Src(0), matrix);
			}

			case EffectType.CrossFadeEffect:
				return new CrossFadeEffectNode(Src(0), Src(1), (float)Prop("CrossFade"));

			case EffectType.AlphaMaskEffect:
				return new AlphaMaskEffectNode(Src(0), Src(1));

			case EffectType.ArithmeticCompositeEffect:
			{
				// Coefficients arrive either as four scalar properties (0..3) or a single Coefficients float[4].
				var multiply = e.GetProperty(0) as float?;
				var source1 = e.GetProperty(1) as float?;
				var source2 = e.GetProperty(2) as float?;
				var offset = e.GetProperty(3) as float?;
				if (multiply is null || source1 is null || source2 is null || offset is null)
				{
					var coefficients = e.GetProperty(0) as float[];
					if (coefficients is null)
					{
						e.GetNamedPropertyMapping("Coefficients", out var coeffProp, out var coeffMapping);
						if (coeffMapping == GraphicsEffectPropertyMapping.Direct)
						{
							coefficients = e.GetProperty(coeffProp) as float[];
						}
					}

					if (coefficients is { Length: 4 })
					{
						multiply = coefficients[0];
						source1 = coefficients[1];
						source2 = coefficients[2];
						offset = coefficients[3];
					}
					else
					{
						return new UnsupportedEffectNode("ArithmeticCompositeEffect", Src(0));
					}
				}

				return new ArithmeticCompositeEffectNode(Src(0), Src(1), multiply.Value, source1.Value, source2.Value, offset.Value);
			}

			case EffectType.WhiteNoiseEffect:
				return new WhiteNoiseEffectNode((Vector2)Prop("Frequency"), (Vector2)Prop("Offset"));

			case EffectType.BorderEffect:
			{
				var extendX = MapEdge((D2D1BorderEdgeMode)(uint)Prop("ExtendX"));
				var extendY = MapEdge((D2D1BorderEdgeMode)(uint)Prop("ExtendY"));

				// Border extends its source to infinity via an edge rule; the repeating unit is the source's own
				// rectangle, so rasterize it at intrinsic size and carry the extend modes on the texture leaf. When
				// there's no intrinsic size (nested effect / unsized brush), tiling degenerates over bounds — the
				// legacy path did the same — so just pass the source through.
				if (e.GetSource(0) is CompositionEffectSourceParameter sourceParameter
					&& resolveSource(sourceParameter.Name) is { IsBackdrop: false, Size: { } size } borderSource)
				{
					return RasterizeSource(borderSource, bounds, size, extendX, extendY);
				}

				return Src(0);
			}

			case EffectType.DistantDiffuseEffect:
				return new LightingEffectNode(Src(0), LightingKind.DistantDiffuse,
					EffectHelpers.GetLightVector(Angle("Azimuth"), Angle("Elevation")), default,
					(Color)Prop("LightColor"), (float)Prop("DiffuseAmount"), 0f, 0f, 0f);

			case EffectType.DistantSpecularEffect:
				return new LightingEffectNode(Src(0), LightingKind.DistantSpecular,
					EffectHelpers.GetLightVector(Angle("Azimuth"), Angle("Elevation")), default,
					(Color)Prop("LightColor"), (float)Prop("SpecularAmount"), (float)Prop("SpecularExponent"), 0f, 0f);

			case EffectType.SpotDiffuseEffect:
			{
				var position = (Vector3)Prop("LightPosition");
				var target = EffectHelpers.CalculateLightTargetVector(position, (Vector3)Prop("LightTarget"));
				return new LightingEffectNode(Src(0), LightingKind.SpotDiffuse, position, target,
					(Color)Prop("LightColor"), (float)Prop("DiffuseAmount"), 0f, (float)Prop("Focus"), Angle("LimitingConeAngle"));
			}

			case EffectType.SpotSpecularEffect:
			{
				var position = (Vector3)Prop("LightPosition");
				var target = EffectHelpers.CalculateLightTargetVector(position, (Vector3)Prop("LightTarget"));
				return new LightingEffectNode(Src(0), LightingKind.SpotSpecular, position, target,
					(Color)Prop("LightColor"), (float)Prop("SpecularAmount"), (float)Prop("SpecularExponent"), (float)Prop("Focus"), Angle("LimitingConeAngle"));
			}

			case EffectType.PointDiffuseEffect:
				return new LightingEffectNode(Src(0), LightingKind.PointDiffuse, (Vector3)Prop("LightPosition"), default,
					(Color)Prop("LightColor"), (float)Prop("DiffuseAmount"), 0f, 0f, 0f);

			case EffectType.PointSpecularEffect:
				return new LightingEffectNode(Src(0), LightingKind.PointSpecular, (Vector3)Prop("LightPosition"), default,
					(Color)Prop("LightColor"), (float)Prop("SpecularAmount"), (float)Prop("SpecularExponent"), 0f, 0f);

			default:
			{
				if (typeof(EffectGraphParser).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(EffectGraphParser).Log().Debug($"Effect '{type}' is not yet neutralized; its source is rendered unmodified.");
				}

				return new UnsupportedEffectNode(type.ToString(), e.GetSourceCount() > 0 ? Src(0) : null);
			}
		}
	}

	// Whole-image composite modes → neutral blend modes; the mapping mirrors SkiaEffectHelpers.ToSkia one-to-one so
	// the two-hop (D2D→neutral→Skia) result equals the old one-hop realization. Unsupported modes fall back to
	// SrcOver (matching the old backend's fallback).
	private static BlendMode MapComposite(D2D1CompositeMode mode) => mode switch
	{
		D2D1CompositeMode.SourceOver => BlendMode.SrcOver,
		D2D1CompositeMode.DestinationOver => BlendMode.DstOver,
		D2D1CompositeMode.SourceIn => BlendMode.SrcIn,
		D2D1CompositeMode.DestinationIn => BlendMode.DstIn,
		D2D1CompositeMode.SourceOut => BlendMode.SrcOut,
		D2D1CompositeMode.DestinationOut => BlendMode.DstOut,
		D2D1CompositeMode.SourceAtop => BlendMode.SrcATop,
		D2D1CompositeMode.DestinationAtop => BlendMode.DstATop,
		D2D1CompositeMode.Xor => BlendMode.Xor,
		D2D1CompositeMode.MaskInvert => BlendMode.Xor, // As of 10.0.25941.1000, the same as Xor
		D2D1CompositeMode.Add => BlendMode.Plus,
		D2D1CompositeMode.Copy => BlendMode.Src,
		_ => BlendMode.SrcOver,
	};

	// BlendEffect modes → neutral blend modes; mirrors SkiaEffectHelpers.ToSkia one-to-one. Modes with no neutral/Skia
	// equivalent fall back to Multiply (matching the old backend's 0xFF fallback).
	private static BlendMode MapBlend(D2D1BlendEffectMode mode) => mode switch
	{
		D2D1BlendEffectMode.Multiply => BlendMode.Multiply,
		D2D1BlendEffectMode.Screen => BlendMode.Screen,
		D2D1BlendEffectMode.Darken => BlendMode.Darken,
		D2D1BlendEffectMode.Lighten => BlendMode.Lighten,
		D2D1BlendEffectMode.ColorBurn => BlendMode.ColorBurn,
		D2D1BlendEffectMode.ColorDodge => BlendMode.ColorDodge,
		D2D1BlendEffectMode.Overlay => BlendMode.Overlay,
		D2D1BlendEffectMode.SoftLight => BlendMode.SoftLight,
		D2D1BlendEffectMode.HardLight => BlendMode.HardLight,
		D2D1BlendEffectMode.Difference => BlendMode.Difference,
		D2D1BlendEffectMode.Exclusion => BlendMode.Exclusion,
		D2D1BlendEffectMode.Hue => BlendMode.Hue,
		D2D1BlendEffectMode.Saturation => BlendMode.Saturation,
		D2D1BlendEffectMode.Color => BlendMode.Color,
		D2D1BlendEffectMode.Luminosity => BlendMode.Luminosity,
		_ => BlendMode.Multiply,
	};

	// BorderEffect edge modes → neutral texture-extend modes.
	private static EdgeExtend MapEdge(D2D1BorderEdgeMode mode) => mode switch
	{
		D2D1BorderEdgeMode.Wrap => EdgeExtend.Wrap,
		D2D1BorderEdgeMode.Mirror => EdgeExtend.Mirror,
		_ => EdgeExtend.Clamp,
	};
}
