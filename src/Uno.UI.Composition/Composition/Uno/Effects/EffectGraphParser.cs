#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Microsoft.UI.Composition;
using Uno.Foundation.Logging;
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

	private static EffectNode RasterizeSource(IEffectSource source, Rect bounds)
	{
		var width = Math.Max(1, (int)Math.Ceiling(bounds.Width));
		var height = Math.Max(1, (int)Math.Ceiling(bounds.Height));

		var texture = DrawingFactory.Current.RenderOffscreen(width, height, session =>
		{
			// The source paints in bounds-space; translate so bounds' origin maps to the offscreen origin.
			if (bounds.X != 0 || bounds.Y != 0)
			{
				session.Translate(-(float)bounds.X, -(float)bounds.Y);
			}

			source.Paint(session, 1f, bounds);
		});

		return new TextureInput(texture);
	}

	private static EffectNode ParseNode(IGraphicsEffectD2D1Interop e, Rect bounds, Func<string, IEffectSource?> resolveSource)
	{
		EffectNode Src(uint i) => Parse(e.GetSource(i), bounds, resolveSource);
		object Prop(string name)
		{
			e.GetNamedPropertyMapping(name, out var index, out _);
			return e.GetProperty(index) ?? throw new InvalidOperationException($"Effect property '{name}' was null.");
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
}
