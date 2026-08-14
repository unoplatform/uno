#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Microsoft.UI.Composition;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.UI.Composition.Effects;

/// <summary>
/// Evaluates a WinUI <see cref="IGraphicsEffect"/> graph directly into drawing-session calls — the neutral
/// replacement for the backend <c>CreateEffectFilter</c>/<c>IEffectFilter</c> SPI. It walks the graph (all D2D
/// reflection lives here) and emits recorded, deferred ops: a colour effect becomes a colour filter, a
/// composite/blend becomes a <see cref="IDrawingSession.SaveLayer(BlendMode, bool)"/>, a gaussian blur becomes a
/// <see cref="IDrawingSession.SaveLayer(in LayerFilter)"/> (or <see cref="IDrawingSession.DrawEffectBackdrop(in
/// LayerFilter, float)"/> when its source is the backdrop), a solid colour a <c>DrawRect</c>, and a brush input a
/// <see cref="IEffectSource.Paint"/>. The backdrop is never read at record time; the retained-rendering layer
/// replays these ops against the real backdrop at present. See <c>specs/effects-neutralization/design.md</c>.
/// </summary>
internal static class EffectGraphEvaluator
{
	/// <summary>Draws the effect graph's output into <paramref name="session"/> over <paramref name="bounds"/>.</summary>
	/// <returns>True if the graph sampled the live backdrop (the brush must then repaint every frame).</returns>
	public static bool Evaluate(object? effect, IDrawingSession session, Func<string, IEffectSource?> resolveSource, Rect bounds, float opacity)
	{
		var ctx = new Context(session, resolveSource, bounds);
		var count = session.Save();
		// Clip to the element bounds: an effect brush paints only within them, and — crucially — this bounds the
		// backdrop-blur layer so its Clamp edge falls on the element, not the whole surface (no bleed from neighbours).
		session.ClipRect(bounds);
		if (opacity < 1f)
		{
			session.SaveLayer(DrawingFactory.Current.CreateColorMatrixColorFilter(AlphaMatrix(opacity)));
		}

		Eval(effect, ctx);
		session.RestoreToCount(count);
		return ctx.UsedBackdrop;
	}

	private sealed class Context
	{
		public Context(IDrawingSession session, Func<string, IEffectSource?> resolve, Rect bounds)
		{
			Session = session;
			Resolve = resolve;
			Bounds = bounds;
		}

		public readonly IDrawingSession Session;
		public readonly Func<string, IEffectSource?> Resolve;
		public readonly Rect Bounds;
		public bool UsedBackdrop;
	}

	// Draws the effect's output into the current session state (over ctx.Bounds).
	private static void Eval(object? effect, Context ctx)
	{
		switch (effect)
		{
			case CompositionEffectSourceParameter param:
			{
				var source = ctx.Resolve(param.Name);
				if (source is null)
				{
					return;
				}

				if (source.IsBackdrop)
				{
					// A bare backdrop leaf: composite the (unblurred) backdrop. Usually it's wrapped in a blur, handled below.
					ctx.UsedBackdrop = true;
					ctx.Session.DrawEffectBackdrop(LayerFilter.Blur(0f, 0f, false), 1f);
				}
				else
				{
					source.Paint(ctx.Session, 1f, ctx.Bounds);
				}
				return;
			}

			case IGraphicsEffectD2D1Interop e:
				EvalNode(e, ctx);
				return;
		}
	}

	private static void EvalNode(IGraphicsEffectD2D1Interop e, Context ctx)
	{
		object Prop(string name)
		{
			e.GetNamedPropertyMapping(name, out var index, out _);
			return e.GetProperty(index) ?? throw new InvalidOperationException($"Effect property '{name}' was null.");
		}

		var session = ctx.Session;
		var type = EffectHelpers.GetEffectType(e.GetEffectId());
		switch (type)
		{
			case EffectType.GaussianBlurEffect:
			{
				var sigma = (float)Prop("BlurAmount");
				var clamp = Prop("BorderMode") is uint bm && bm == 1; // EffectBorderMode.Hard
				var source = e.GetSource(0);
				if (source is CompositionEffectSourceParameter p && ctx.Resolve(p.Name) is { IsBackdrop: true })
				{
					// Blur of the live backdrop → the deferred backdrop-blur layer (composited at present).
					ctx.UsedBackdrop = true;
					session.DrawEffectBackdrop(LayerFilter.Blur(sigma, sigma, clamp), 1f);
				}
				else
				{
					var count = session.Save();
					session.SaveLayer(LayerFilter.Blur(sigma, sigma, clamp));
					Eval(source, ctx);
					session.RestoreToCount(count);
				}
				return;
			}

			case EffectType.ColorSourceEffect:
				session.DrawRect(ctx.Bounds, (Color)Prop("Color"));
				return;

			case EffectType.CompositeEffect:
			{
				var mode = MapComposite((D2D1CompositeMode)(uint)Prop("Mode"));
				var count = e.GetSourceCount();
				Eval(e.GetSource(0), ctx);
				for (uint i = 1; i < count; i++)
				{
					var save = session.Save();
					session.SaveLayer(mode);
					Eval(e.GetSource(i), ctx);
					session.RestoreToCount(save);
				}
				return;
			}

			case EffectType.BlendEffect:
			{
				var mode = MapBlend((D2D1BlendEffectMode)(uint)Prop("Mode"));
				Eval(e.GetSource(0), ctx); // background
				var save = session.Save();
				session.SaveLayer(mode);
				Eval(e.GetSource(1), ctx); // foreground blended over background
				session.RestoreToCount(save);
				return;
			}

			default:
			{
				// A per-pixel colour effect → a colour filter over the (single) source; other effects render their
				// first source unmodified (logged). Acrylic never hits the default arm.
				if (TryColorMatrix(type, e, out var matrix))
				{
					var count = session.Save();
					session.SaveLayer(DrawingFactory.Current.CreateColorMatrixColorFilter(matrix));
					Eval(e.GetSource(0), ctx);
					session.RestoreToCount(count);
					return;
				}

				if (typeof(EffectGraphEvaluator).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(EffectGraphEvaluator).Log().Debug($"Effect '{type}' is not yet evaluated; rendering its source unmodified.");
				}
				if (e.GetSourceCount() > 0)
				{
					Eval(e.GetSource(0), ctx);
				}
				return;
			}
		}
	}

	// The per-pixel colour effects that are a 4×5 colour matrix. Others (transfer curves, lighting, noise) return false.
	private static bool TryColorMatrix(EffectType type, IGraphicsEffectD2D1Interop e, out float[] matrix)
	{
		object Prop(string name)
		{
			e.GetNamedPropertyMapping(name, out var index, out _);
			return e.GetProperty(index) ?? throw new InvalidOperationException($"Effect property '{name}' was null.");
		}

		switch (type)
		{
			case EffectType.OpacityEffect:
				matrix = AlphaMatrix((float)Prop("Opacity"));
				return true;

			case EffectType.ColorMatrixEffect:
			{
				var m = (float[])Prop("ColorMatrix"); // D2D 5×4
				matrix = new[]
				{
					m[0],  m[1],  m[2],  m[3],  m[16],
					m[4],  m[5],  m[6],  m[7],  m[17],
					m[8],  m[9],  m[10], m[11], m[18],
					m[12], m[13], m[14], m[15], m[19],
				};
				return true;
			}

			default:
				matrix = null!;
				return false;
		}
	}

	private static float[] AlphaMatrix(float a) => new[]
	{
		1f, 0f, 0f, 0f, 0f,
		0f, 1f, 0f, 0f, 0f,
		0f, 0f, 1f, 0f, 0f,
		0f, 0f, 0f, a,  0f,
	};

	private static BlendMode MapComposite(D2D1CompositeMode mode) => mode switch
	{
		D2D1CompositeMode.SourceOver => BlendMode.SrcOver,
		D2D1CompositeMode.SourceIn => BlendMode.SrcIn,
		D2D1CompositeMode.DestinationIn => BlendMode.DstIn,
		D2D1CompositeMode.DestinationOut => BlendMode.DstOut,
		D2D1CompositeMode.Add => BlendMode.Plus,
		_ => BlendMode.SrcOver,
	};

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
