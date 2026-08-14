#nullable enable

using System;
using System.Collections.Generic;
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
/// property values, the recursive source walk — lives here, once. The render path then evaluates typed nodes and
/// no backend ever interprets an <c>IGraphicsEffect</c>. See <c>specs/effects-neutralization/design.md</c>.
/// </summary>
internal static class EffectGraphParser
{
	/// <summary>Parses <paramref name="effect"/> (a graph node or a source-parameter leaf) into an <see cref="EffectNode"/>.</summary>
	public static EffectNode Parse(object? effect, Func<string, IEffectSource?> resolveSource)
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

				return source.IsBackdrop ? new BackdropInput() : new BrushInput(source);
			}

			case IGraphicsEffectD2D1Interop interop:
				return ParseNode(interop, resolveSource);

			default:
				return new UnsupportedEffectNode(effect?.GetType().Name ?? "null", null);
		}
	}

	private static EffectNode ParseNode(IGraphicsEffectD2D1Interop e, Func<string, IEffectSource?> resolveSource)
	{
		EffectNode Src(uint i) => Parse(e.GetSource(i), resolveSource);
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
				// colour-matrix filter uses (rows R,G,B,A; last column = bias). Matches the current Skia realization.
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

				// Pass the first source through so its content still draws (better than dropping the subtree).
				return new UnsupportedEffectNode(type.ToString(), e.GetSourceCount() > 0 ? Src(0) : null);
			}
		}
	}

	// Whole-image composite modes → the neutral session's blend modes; unmapped modes fall back to SrcOver (the
	// WinUI default), matching the current Skia behaviour. Widened as the session gains more blend modes.
	private static BlendMode MapComposite(D2D1CompositeMode mode) => mode switch
	{
		D2D1CompositeMode.SourceOver => BlendMode.SrcOver,
		D2D1CompositeMode.SourceIn => BlendMode.SrcIn,
		D2D1CompositeMode.DestinationIn => BlendMode.DstIn,
		D2D1CompositeMode.DestinationOut => BlendMode.DstOut,
		D2D1CompositeMode.Add => BlendMode.Plus,
		_ => BlendMode.SrcOver,
	};

	// Photoshop-style blend modes; the neutral session supports only Multiply today, so others fall back to it
	// (matches the current Skia behaviour). Widened as the session gains more blend modes.
	private static BlendMode MapBlend(D2D1BlendEffectMode mode) => mode switch
	{
		_ => BlendMode.Multiply,
	};
}
