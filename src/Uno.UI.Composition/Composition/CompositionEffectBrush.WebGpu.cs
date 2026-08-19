#nullable enable

using System;
using Windows.UI;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

// WebGPU path. The WebGPU backend can't use Skia's SKImageFilter tree, so walk the IGraphicsEffect
// graph into a compact "recipe": a single source (image/backdrop/color) + a composed 4×5 color matrix the backend
// applies. Covers single-source per-pixel color effects (grayscale, invert, hue, sepia, opacity, color-matrix, …);
// blur passes through unapplied, and multi-source / lighting / transform / border / mask / noise / nonlinear
// effects are unsupported (recipe fails).
public partial class CompositionEffectBrush
{
	internal bool TryGetWebGpuEffectRecipe(out CompositionBrush? source, out bool isBackdrop, out Color? solidColor, out float[] colorMatrix)
	{
		source = null;
		isBackdrop = false;
		solidColor = null;
		var acc = Identity5x5();
		bool ok = WalkRecipe(_effect, ref acc, ref source, ref isBackdrop, ref solidColor);
		colorMatrix = To4x5(acc);
		return ok;
	}

	private bool WalkRecipe(object? node, ref float[] acc, ref CompositionBrush? source, ref bool isBackdrop, ref Color? solidColor)
	{
		switch (node)
		{
			case CompositionEffectSourceParameter p:
			{
				var brush = GetSourceParameter(p.Name);
				if (brush is CompositionBackdropBrush) { isBackdrop = true; return true; }
				source = brush;
				return brush is not null;
			}
			case IGraphicsEffectD2D1Interop e:
			{
				switch (EffectHelpers.GetEffectType(e.GetEffectId()))
				{
					case EffectType.GaussianBlurEffect: // blur not applied on the WebGPU path yet — pass through
						return e.GetSourceCount() == 1 && WalkRecipe(e.GetSource(0), ref acc, ref source, ref isBackdrop, ref solidColor);

					case EffectType.ColorSourceEffect:
						e.GetNamedPropertyMapping("Color", out uint cprop, out _);
						solidColor = (Color)(e.GetProperty(cprop) ?? Colors.Transparent);
						return true;

					case EffectType.GrayscaleEffect: return Compose(e, Grayscale(), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.InvertEffect: return Compose(e, Invert(), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.LuminanceToAlphaEffect: return Compose(e, LuminanceToAlpha(), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.HueRotationEffect: return Compose(e, HueRotation(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.SaturationEffect: return Compose(e, Saturation(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.SepiaEffect: return Compose(e, Sepia(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.TemperatureAndTintEffect: return Compose(e, Temperature(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.OpacityEffect: return Compose(e, Opacity(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.ExposureEffect: return Compose(e, Exposure(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.TintEffect: return Compose(e, Tint(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.LinearTransferEffect: return Compose(e, LinearTransfer(e), ref acc, ref source, ref isBackdrop, ref solidColor);
					case EffectType.ColorMatrixEffect: return Compose(e, ColorMatrix(e), ref acc, ref source, ref isBackdrop, ref solidColor);

					default:
						return false; // unsupported effect → fall back (renders nothing)
				}
			}
			default:
				return false;
		}
	}

	private bool Compose(IGraphicsEffectD2D1Interop e, float[] m, ref float[] acc, ref CompositionBrush? source, ref bool isBackdrop, ref Color? solidColor)
	{
		if (e.GetSourceCount() != 1) { return false; }
		acc = Mul5x5(acc, To5x5(m));
		return WalkRecipe(e.GetSource(0), ref acc, ref source, ref isBackdrop, ref solidColor);
	}

	private static float Prop(IGraphicsEffectD2D1Interop e, string name)
	{
		e.GetNamedPropertyMapping(name, out uint idx, out _);
		return (float)(e.GetProperty(idx) ?? 0f);
	}

	// Applies a 4×5 color matrix (row-major: R,G,B,A rows; cols r,g,b,a,offset; offset 0..1) to a color.
	internal static Color ApplyColorMatrix(Color c, float[] m)
	{
		float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f;
		float nr = m[0] * r + m[1] * g + m[2] * b + m[3] * a + m[4];
		float ng = m[5] * r + m[6] * g + m[7] * b + m[8] * a + m[9];
		float nb = m[10] * r + m[11] * g + m[12] * b + m[13] * a + m[14];
		float na = m[15] * r + m[16] * g + m[17] * b + m[18] * a + m[19];
		static byte B(float v) => (byte)Math.Clamp(v * 255f + 0.5f, 0f, 255f);
		return Color.FromArgb(B(na), B(nr), B(ng), B(nb));
	}

	// 4×5 (row-major: R,G,B,A rows; cols r,g,b,a,offset). offset in 0..1.
	private static float[] Grayscale() => new float[]
	{
		0.21f, 0.72f, 0.07f, 0, 0,
		0.21f, 0.72f, 0.07f, 0, 0,
		0.21f, 0.72f, 0.07f, 0, 0,
		0, 0, 0, 1, 0,
	};

	private static float[] Invert() => new float[]
	{
		-1, 0, 0, 0, 1,
		0, -1, 0, 0, 1,
		0, 0, -1, 0, 1,
		0, 0, 0, 1, 0,
	};

	private static float[] LuminanceToAlpha() => new float[]
	{
		0, 0, 0, 0, 0,
		0, 0, 0, 0, 0,
		0, 0, 0, 0, 0,
		0.2126f, 0.7152f, 0.0722f, 0, 0,
	};

	private static float[] Opacity(IGraphicsEffectD2D1Interop e)
	{
		float o = Prop(e, "Opacity");
		return new float[] { 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, o, 0 };
	}

	private static float[] Exposure(IGraphicsEffectD2D1Interop e)
	{
		float m = MathF.Pow(2f, Prop(e, "Exposure"));
		return new float[] { m, 0, 0, 0, 0, 0, m, 0, 0, 0, 0, 0, m, 0, 0, 0, 0, 0, 1, 0 };
	}

	private static float[] Tint(IGraphicsEffectD2D1Interop e)
	{
		e.GetNamedPropertyMapping("Color", out uint cprop, out _);
		var c = (Color)(e.GetProperty(cprop) ?? Colors.White);
		float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f, a = c.A / 255f;
		return new float[] { r, 0, 0, 0, 0, 0, g, 0, 0, 0, 0, 0, b, 0, 0, 0, 0, 0, a, 0 };
	}

	private static float[] Sepia(IGraphicsEffectD2D1Interop e)
	{
		float inv = 1 - Prop(e, "Intensity");
		return new float[]
		{
			0.393f + 0.607f * inv, 0.769f - 0.769f * inv, 0.189f - 0.189f * inv, 0, 0,
			0.349f - 0.349f * inv, 0.686f + 0.314f * inv, 0.168f - 0.168f * inv, 0, 0,
			0.272f - 0.272f * inv, 0.534f - 0.534f * inv, 0.131f + 0.869f * inv, 0, 0,
			0, 0, 0, 1, 0,
		};
	}

	private static float[] Saturation(IGraphicsEffectD2D1Interop e)
	{
		float s = MathF.Min(Prop(e, "Saturation"), 2);
		return new float[]
		{
			0.2126f + 0.7874f * s, 0.7152f - 0.7152f * s, 0.0722f - 0.0722f * s, 0, 0,
			0.2126f - 0.2126f * s, 0.7152f + 0.2848f * s, 0.0722f - 0.0722f * s, 0, 0,
			0.2126f - 0.2126f * s, 0.7152f - 0.7152f * s, 0.0722f + 0.9278f * s, 0, 0,
			0, 0, 0, 1, 0,
		};
	}

	private static float[] Temperature(IGraphicsEffectD2D1Interop e)
	{
		var gains = TempAndTintHelpers.TempTintToGains(Prop(e, "Temperature"), Prop(e, "Tint"));
		return new float[]
		{
			gains.RedGain, 0, 0, 0, 0,
			0, 1, 0, 0, 0,
			0, 0, gains.BlueGain, 0, 0,
			0, 0, 0, 1, 0,
		};
	}

	private static float[] HueRotation(IGraphicsEffectD2D1Interop e)
	{
		e.GetNamedPropertyMapping("Angle", out uint idx, out GraphicsEffectPropertyMapping mapping);
		float angle = (float)(e.GetProperty(idx) ?? 0f);
		if (mapping == GraphicsEffectPropertyMapping.RadiansToDegrees) { angle *= 180f / MathF.PI; }
		float c = MathF.Cos(angle), s = MathF.Sin(angle);
		return new float[]
		{
			0.2127f + c * 0.7873f - s * 0.2127f, 0.715f - c * 0.715f - s * 0.715f, 0.072f - c * 0.072f + s * 0.928f, 0, 0,
			0.2127f - c * 0.213f + s * 0.143f, 0.715f + c * 0.285f + s * 0.140f, 0.072f - c * 0.072f - s * 0.283f, 0, 0,
			0.2127f - c * 0.213f - s * 0.787f, 0.715f - c * 0.715f + s * 0.715f, 0.072f + c * 0.928f + s * 0.072f, 0, 0,
			0, 0, 0, 1, 0,
		};
	}

	private static float[] LinearTransfer(IGraphicsEffectD2D1Interop e)
	{
		float Slope(string p, string d) => (bool)(e.GetProperty(Idx(e, d)) ?? false) ? 1f : Prop(e, p);
		float Off(string p, string d) => (bool)(e.GetProperty(Idx(e, d)) ?? false) ? 0f : Prop(e, p);
		return new float[]
		{
			Slope("RedSlope", "RedDisable"), 0, 0, 0, Off("RedOffset", "RedDisable"),
			0, Slope("GreenSlope", "GreenDisable"), 0, 0, Off("GreenOffset", "GreenDisable"),
			0, 0, Slope("BlueSlope", "BlueDisable"), 0, Off("BlueOffset", "BlueDisable"),
			0, 0, 0, Slope("AlphaSlope", "AlphaDisable"), Off("AlphaOffset", "AlphaDisable"),
		};
	}

	private static uint Idx(IGraphicsEffectD2D1Interop e, string name)
	{
		e.GetNamedPropertyMapping(name, out uint idx, out _);
		return idx;
	}

	private static float[] ColorMatrix(IGraphicsEffectD2D1Interop e)
	{
		e.GetNamedPropertyMapping("ColorMatrix", out uint idx, out _);
		var m = (float[])(e.GetProperty(idx) ?? new float[20]);
		return new float[]
		{
			m[0], m[1], m[2], m[3], m[16],
			m[4], m[5], m[6], m[7], m[17],
			m[8], m[9], m[10], m[11], m[18],
			m[12], m[13], m[14], m[15], m[19],
		};
	}

	// --- 5×5 affine color-transform composition (row-major). Last row is [0,0,0,0,1]. ---
	private static float[] Identity5x5() => new float[]
	{
		1, 0, 0, 0, 0,
		0, 1, 0, 0, 0,
		0, 0, 1, 0, 0,
		0, 0, 0, 1, 0,
		0, 0, 0, 0, 1,
	};

	private static float[] To5x5(float[] m4x5)
	{
		var r = new float[25];
		Array.Copy(m4x5, 0, r, 0, 20);
		r[24] = 1;
		return r;
	}

	private static float[] To4x5(float[] m5x5)
	{
		var r = new float[20];
		Array.Copy(m5x5, 0, r, 0, 20);
		return r;
	}

	private static float[] Mul5x5(float[] a, float[] b)
	{
		var r = new float[25];
		for (int i = 0; i < 5; i++)
		{
			for (int j = 0; j < 5; j++)
			{
				float sum = 0;
				for (int k = 0; k < 5; k++) { sum += a[i * 5 + k] * b[k * 5 + j]; }
				r[i * 5 + j] = sum;
			}
		}
		return r;
	}
}
