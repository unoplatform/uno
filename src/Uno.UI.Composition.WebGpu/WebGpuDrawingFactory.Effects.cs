// The effect-graph evaluator: renders a neutral effect tree to a texture by offscreen composition, one WebGpuFrame per
// stage, and wraps the result as the effect filter the recorder draws.
#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Uno.WebGpu.Native;
using static Uno.WebGpu.Native.WGPU;
using Uno.UI.Composition.Drawing;
using Uno.Foundation.Logging;
using Windows.Graphics.Effects.Interop;
using Windows.Foundation;
using WColor = Windows.UI.Color;

namespace Uno.UI.Composition.WebGpu;

public sealed partial class WebGpuDrawingFactory
{
	// True if the tree reads the (deferred) backdrop; those go through the acrylic recipe instead.
	private static bool ContainsBackdrop(EffectNode node) => node switch
	{
		SourceInput => true,
		ColorMatrixEffectNode n => ContainsBackdrop(n.Source),
		BlurEffectNode n => ContainsBackdrop(n.Source),
		ModulateEffectNode n => ContainsBackdrop(n.Source),
		LuminanceToAlphaEffectNode n => ContainsBackdrop(n.Source),
		ContrastEffectNode n => ContainsBackdrop(n.Source),
		LinearTransferEffectNode n => ContainsBackdrop(n.Source),
		GammaTransferEffectNode n => ContainsBackdrop(n.Source),
		BlendEffectNode n => ContainsBackdrop(n.Background) || ContainsBackdrop(n.Foreground),
		CompositeEffectNode n => n.Sources.Any(ContainsBackdrop),
		ArithmeticCompositeEffectNode n => ContainsBackdrop(n.Background) || ContainsBackdrop(n.Foreground),
		CrossFadeEffectNode n => ContainsBackdrop(n.SourceA) || ContainsBackdrop(n.SourceB),
		AlphaMaskEffectNode n => ContainsBackdrop(n.Source) || ContainsBackdrop(n.Mask),
		UnsupportedEffectNode n => n.Source is not null && ContainsBackdrop(n.Source),
		_ => false,
	};

	// BlendMode → CompositeBlendWgsl mode id (stable, independent of the enum's ordinals).
	private static int BlendShaderId(BlendMode mode) => mode switch
	{
		BlendMode.SrcOver => 0,
		BlendMode.Src => 1,
		BlendMode.Plus => 2,
		BlendMode.Multiply => 4,
		BlendMode.DstIn => 5,
		BlendMode.DstOut => 6,
		BlendMode.SrcIn => 7,
		BlendMode.DstOver => 8,
		BlendMode.SrcOut => 9,
		BlendMode.SrcATop => 10,
		BlendMode.DstATop => 11,
		BlendMode.Xor => 12,
		BlendMode.Screen => 13,
		BlendMode.Darken => 14,
		BlendMode.Lighten => 15,
		BlendMode.ColorBurn => 16,
		BlendMode.ColorDodge => 17,
		BlendMode.Overlay => 18,
		BlendMode.SoftLight => 19,
		BlendMode.HardLight => 20,
		BlendMode.Difference => 21,
		BlendMode.Exclusion => 22,
		BlendMode.Hue => 23,
		BlendMode.Saturation => 24,
		BlendMode.Color => 25,
		BlendMode.Luminosity => 26,
		_ => 0,
	};

	private ITexture RunBlend(WebGpuTexture bg, WebGpuTexture fg, int shaderMode)
	{
		int w = Math.Max(bg.PixelWidth, fg.PixelWidth), h = Math.Max(bg.PixelHeight, fg.PixelHeight);
		var surface = new WebGpuRenderSurface(_device, w, h);
		new WebGpuFrame(_device, surface).Effects.BlendInto(bg, fg, shaderMode);
		var (tex, view) = surface.DetachColor();
		surface.Dispose();
		return new WebGpuTexture(_device, tex, view, w, h);
	}

	// out = k0*A + k1*B + k2*(A*B) + k3 (or A masked by B's alpha) into a fresh offscreen texture.
	private ITexture RunCombine(WebGpuTexture a, WebGpuTexture b, float k0, float k1, float k2, float k3, bool alphaMask)
	{
		int w = Math.Max(a.PixelWidth, b.PixelWidth), h = Math.Max(a.PixelHeight, b.PixelHeight);
		var surface = new WebGpuRenderSurface(_device, w, h);
		new WebGpuFrame(_device, surface).Effects.CombineInto(a, b, k0, k1, k2, k3, alphaMask);
		var (tex, view) = surface.DetachColor();
		surface.Dispose();
		return new WebGpuTexture(_device, tex, view, w, h);
	}

	private ITexture RunNoise(int w, int h, System.Numerics.Vector2 freq, System.Numerics.Vector2 offset)
	{
		var surface = new WebGpuRenderSurface(_device, w, h);
		new WebGpuFrame(_device, surface).Effects.NoiseInto(freq.X, freq.Y, offset.X, offset.Y, w, h);
		var (tex, view) = surface.DetachColor();
		surface.Dispose();
		return new WebGpuTexture(_device, tex, view, w, h);
	}

	private WebGpuTexture Blur(WebGpuTexture src, float sigma)
	{
		int w = src.PixelWidth, h = src.PixelHeight;
		var surface = new WebGpuRenderSurface(_device, w, h);
		new WebGpuFrame(_device, surface).Effects.BlurInto(src, sigma, sigma);
		var (tex, view) = surface.DetachColor();
		surface.Dispose();
		return new WebGpuTexture(_device, tex, view, w, h);
	}

	// Contrast / GammaTransfer: a per-channel function of one input.
	private ITexture RunColorFunc(WebGpuTexture src, float[] u20)
	{
		int w = src.PixelWidth, h = src.PixelHeight;
		var surface = new WebGpuRenderSurface(_device, w, h);
		new WebGpuFrame(_device, surface).Effects.ColorFuncInto(src, u20);
		var (tex, view) = surface.DetachColor();
		surface.Dispose();
		return new WebGpuTexture(_device, tex, view, w, h);
	}

	// General evaluator for NON-backdrop trees (leaves + colour-matrix + blur + blend/composite + Unsupported→source).
	// Renders the tree to a texture by offscreen composition, or returns null for any node it does not handle, which
	// leaves the caller on the acrylic/recipe path.
	//
	// The returned texture carries a reference the caller owns and must release, which is what lets every arm release
	// its children uniformly — an arm cannot otherwise tell a tree-owned TextureInput leaf from one it just rendered.
	private ITexture TryEvaluateTree(EffectNode node, Rect bounds)
	{
		switch (node)
		{
			case TextureInput t:
				{
					if (t.ExtendX == EdgeExtend.None && t.ExtendY == EdgeExtend.None)
					{
						// The tree owns this leaf (DisposeTree releases it), so hand the caller its own reference.
						t.Texture.AddRef();
						return t.Texture;
					}

					// A BorderEffect extends its source past its own rect, and downstream nodes sample over the whole
					// bounds, so realize the extended fill as a bounds-sized input here.
					int tw = Math.Max(1, (int)Math.Round(bounds.Width)), th = Math.Max(1, (int)Math.Round(bounds.Height));
					return RenderOffscreen(tw, th, s => s.DrawImageTiled(t.Texture, new Rect(0, 0, tw, th), t.ExtendX, t.ExtendY));
				}
			case ColorInput c:
				{
					int cw = Math.Max(1, (int)Math.Round(bounds.Width)), ch = Math.Max(1, (int)Math.Round(bounds.Height));
					return RenderOffscreen(cw, ch, s => s.DrawRect(new Rect(0, 0, cw, ch), c.Color));
				}
			case ColorMatrixEffectNode cm:
				{
					if (TryEvaluateTree(cm.Source, bounds) is not { } src) { return null; }
					int w = src.PixelWidth, h = src.PixelHeight;
					using var filter = CreateColorMatrixColorFilter(cm.Matrix);
					var result = RenderOffscreen(w, h, s => s.DrawImage(src, 0, 0, filter));
					src.Release();
					return result;
				}
			case BlendEffectNode blend:
				{
					if (TryEvaluateTree(blend.Background, bounds) is not WebGpuTexture bg) { return null; }
					if (TryEvaluateTree(blend.Foreground, bounds) is not WebGpuTexture fg) { bg.Release(); return null; }
					var result = RunBlend(bg, fg, BlendShaderId(blend.Mode));
					bg.Release();
					fg.Release();
					return result;
				}
			case CompositeEffectNode comp:
				{
					if (comp.Sources.Count == 0) { return null; }
					if (TryEvaluateTree(comp.Sources[0], bounds) is not WebGpuTexture acc) { return null; }
					int id = BlendShaderId(comp.Mode);
					for (int i = 1; i < comp.Sources.Count; i++)
					{
						if (TryEvaluateTree(comp.Sources[i], bounds) is not WebGpuTexture next) { acc.Release(); return null; }
						var folded = RunBlend(acc, next, id) as WebGpuTexture;
						acc.Release();
						next.Release();
						if (folded is null) { return null; }
						acc = folded;
					}

					return acc;
				}
			case CrossFadeEffectNode cf:
				{
					if (TryEvaluateTree(cf.SourceA, bounds) is not WebGpuTexture a) { return null; }
					if (TryEvaluateTree(cf.SourceB, bounds) is not WebGpuTexture bb) { a.Release(); return null; }
					var result = RunCombine(a, bb, 1f - cf.Weight, cf.Weight, 0f, 0f, alphaMask: false);
					a.Release();
					bb.Release();
					return result;
				}
			case ArithmeticCompositeEffectNode ar:
				{
					if (TryEvaluateTree(ar.Foreground, bounds) is not WebGpuTexture fg) { return null; }
					if (TryEvaluateTree(ar.Background, bounds) is not WebGpuTexture bg) { fg.Release(); return null; }
					var result = RunCombine(fg, bg, ar.Source1, ar.Source2, ar.Multiply, ar.Offset, alphaMask: false);
					fg.Release();
					bg.Release();
					return result;
				}
			case AlphaMaskEffectNode am:
				{
					if (TryEvaluateTree(am.Source, bounds) is not WebGpuTexture src2) { return null; }
					if (TryEvaluateTree(am.Mask, bounds) is not WebGpuTexture mask) { src2.Release(); return null; }
					var result = RunCombine(src2, mask, 0f, 0f, 0f, 0f, alphaMask: true);
					src2.Release();
					mask.Release();
					return result;
				}
			case WhiteNoiseEffectNode n:
				{
					int w = Math.Max(1, (int)Math.Round(bounds.Width)), h = Math.Max(1, (int)Math.Round(bounds.Height));
					return RunNoise(w, h, n.Frequency, n.Offset);
				}
			case ContrastEffectNode ct:
				{
					if (TryEvaluateTree(ct.Source, bounds) is not WebGpuTexture s) { return null; }
					var u = new float[20];
					u[0] = 0f; u[1] = ct.Contrast; u[2] = ct.Clamp ? 1f : 0f;
					var result = RunColorFunc(s, u);
					s.Release();
					return result;
				}
			case GammaTransferEffectNode g:
				{
					if (TryEvaluateTree(g.Source, bounds) is not WebGpuTexture s) { return null; }
					var u = new float[20];
					u[0] = 1f; u[2] = g.Clamp ? 1f : 0f;
					u[4] = g.Amplitudes[0]; u[5] = g.Amplitudes[1]; u[6] = g.Amplitudes[2]; u[7] = g.Amplitudes[3];
					u[8] = g.Exponents[0]; u[9] = g.Exponents[1]; u[10] = g.Exponents[2]; u[11] = g.Exponents[3];
					u[12] = g.Offsets[0]; u[13] = g.Offsets[1]; u[14] = g.Offsets[2]; u[15] = g.Offsets[3];
					u[16] = g.Disable[0] ? 1f : 0f; u[17] = g.Disable[1] ? 1f : 0f; u[18] = g.Disable[2] ? 1f : 0f; u[19] = g.Disable[3] ? 1f : 0f;
					var result = RunColorFunc(s, u);
					s.Release();
					return result;
				}
			case BlurEffectNode b:
				{
					if (TryEvaluateTree(b.Source, bounds) is not WebGpuTexture src) { return null; }
					if (b.Sigma <= 0f) { return src; }
					int w = src.PixelWidth, h = src.PixelHeight;
					// A soft border — D2D's default — fades to transparent past the source edge, but the pyramid samples
					// clamp-to-edge and would smear the edge texel outwards instead. Pad the source so the clamp has
					// transparency to read, then crop the fade back to the source rect the way Skia crops to bounds.
					// The pyramid itself is shared with the per-frame acrylic backdrop, which does want the clamp.
					var margin = b.ClampEdge ? 0 : Math.Min(256, (int)MathF.Ceiling(b.Sigma * 3f));
					if (margin == 0)
					{
						var blurredOnly = Blur(src, b.Sigma);
						src.Release();
						return blurredOnly;
					}

					var padded = (WebGpuTexture)RenderOffscreen(w + (2 * margin), h + (2 * margin), s => s.DrawImage(src, margin, margin));
					src.Release();
					var blurred = Blur(padded, b.Sigma);
					padded.Release();
					var cropped = RenderOffscreen(w, h, s => s.DrawImage(blurred, -margin, -margin));
					blurred.Release();
					return cropped;
				}
			case UnsupportedEffectNode u:
				// Pass-through: the child's reference transfers straight to our caller.
				return u.Source is null ? null : TryEvaluateTree(u.Source, bounds);
			default:
				return null;   // SourceInput / Blend / Composite / … — later phases
		}
	}

	// Fuses the neutral EffectNode tree (Uno's parser output) into a backend filter. First tries the general
	// non-backdrop evaluator (renders the whole tree to a texture); otherwise realizes the acrylic shape
	// (a gaussian-blurred backdrop + tint/luminosity colours); any other tree returns null so CompositionEffectBrush
	// falls back to the recipe path. Structure-matches the acrylic graph: the outer Blend's ColorInput foreground is
	// the tint, the inner Blend's is the luminosity colour.
	public IEffectFilter CreateEffectFilter(EffectNode tree, Rect bounds)
	{
		if (!ContainsBackdrop(tree) && TryEvaluateTree(tree, bounds) is { } evaluated)
		{
			return new WebGpuEffectFilter { EvaluatedTexture = evaluated, EvaluatedBounds = bounds };
		}

		float sigma = 0f;
		WColor tint = default, lum = default;
		bool sawColorSource = false;
		bool sawBackdrop = false;

		void Walk(EffectNode node)
		{
			switch (node)
			{
				case SourceInput:
					sawBackdrop = true;
					break;
				case BlurEffectNode blur:
					sigma = MathF.Max(sigma, blur.Sigma);
					Walk(blur.Source);
					break;
				case BlendEffectNode blend:
					if (blend.Foreground is ColorInput colorInput)
					{
						sawColorSource = true;
						if (blend.Background is BlendEffectNode) { tint = colorInput.Color; } else { lum = colorInput.Color; }
					}
					Walk(blend.Background);
					Walk(blend.Foreground);
					break;
				default:
					foreach (var child in node.Children) { Walk(child); }
					break;
			}
		}

		Walk(tree);

		if ((sigma > 0f || sawColorSource) && sawBackdrop)
		{
			return new WebGpuEffectFilter { SigmaX = sigma, SigmaY = sigma, Color = tint, LumColor = lum, Noise = 0.02f };
		}

		return null;
	}
}
