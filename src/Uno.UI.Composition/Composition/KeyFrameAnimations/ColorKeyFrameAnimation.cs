#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace Microsoft.UI.Composition;

public partial class ColorKeyFrameAnimation : KeyFrameAnimation
{
#if !__APPLE_UIKIT__
	private readonly SortedDictionary<float, AnimationKeyFrame<Color>> _keyFrames = new();

	internal ColorKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	public CompositionColorSpace InterpolationColorSpace { get; set; } = CompositionColorSpace.Auto;

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, Color value)
		=> InsertKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	public void InsertKeyFrame(float normalizedProgressKey, Color value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value, EasingFunction = easingFunction };

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);

		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = new()
			{
				Value = (Color)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString()),
				EasingFunction = Compositor.GetDefaultEasingFunction()
			};
		}

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		var colorSpace = InterpolationColorSpace;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Color Lerp(AnimationKeyFrame<Color> value1, AnimationKeyFrame<Color> value2, float amount)
			=> LerpColor(value1.Value, value2.Value, value2.EasingFunction.Ease(amount), colorSpace);

		_keyframeEvaluator = new KeyFrameEvaluator<Color>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue.Value;
	}

	// TODO Uno: RgbLinear/HslLinear should interpolate in linear-light space; they currently fall back
	// to their gamma (Rgb/Hsl) counterparts. Lottie output uses Rgb, which is exact.
	private static Color LerpColor(Color from, Color to, float amount, CompositionColorSpace colorSpace)
		=> colorSpace is CompositionColorSpace.Hsl or CompositionColorSpace.HslLinear
			? LerpHsl(from, to, amount)
			: LerpRgb(from, to, amount);

	private static byte LerpChannel(byte from, byte to, float amount)
		=> (byte)Math.Clamp(from + (to - from) * amount, 0f, 255f);

	private static Color LerpRgb(Color from, Color to, float amount)
		=> Color.FromArgb(
			LerpChannel(from.A, to.A, amount),
			LerpChannel(from.R, to.R, amount),
			LerpChannel(from.G, to.G, amount),
			LerpChannel(from.B, to.B, amount));

	private static Color LerpHsl(Color from, Color to, float amount)
	{
		var (h1, s1, l1) = ToHsl(from);
		var (h2, s2, l2) = ToHsl(to);

		// Interpolate hue along the shortest arc.
		var dh = h2 - h1;
		if (dh > 180f)
		{
			dh -= 360f;
		}
		else if (dh < -180f)
		{
			dh += 360f;
		}

		var h = (h1 + dh * amount + 360f) % 360f;
		var s = s1 + (s2 - s1) * amount;
		var l = l1 + (l2 - l1) * amount;

		return FromHsl(LerpChannel(from.A, to.A, amount), h, s, l);
	}

	private static (float H, float S, float L) ToHsl(Color color)
	{
		float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f;
		var max = Math.Max(r, Math.Max(g, b));
		var min = Math.Min(r, Math.Min(g, b));
		var l = (max + min) / 2f;
		float h = 0f, s = 0f;
		var d = max - min;
		if (d > 0f)
		{
			s = l > 0.5f ? d / (2f - max - min) : d / (max + min);
			if (max == r)
			{
				h = (g - b) / d + (g < b ? 6f : 0f);
			}
			else if (max == g)
			{
				h = (b - r) / d + 2f;
			}
			else
			{
				h = (r - g) / d + 4f;
			}

			h *= 60f;
		}

		return (h, s, l);
	}

	private static Color FromHsl(byte a, float h, float s, float l)
	{
		float r, g, b;
		if (s == 0f)
		{
			r = g = b = l;
		}
		else
		{
			var q = l < 0.5f ? l * (1f + s) : l + s - l * s;
			var p = 2f * l - q;
			var hk = h / 360f;
			r = HueToRgb(p, q, hk + 1f / 3f);
			g = HueToRgb(p, q, hk);
			b = HueToRgb(p, q, hk - 1f / 3f);
		}

		return Color.FromArgb(
			a,
			(byte)Math.Clamp(r * 255f, 0f, 255f),
			(byte)Math.Clamp(g * 255f, 0f, 255f),
			(byte)Math.Clamp(b * 255f, 0f, 255f));
	}

	private static float HueToRgb(float p, float q, float t)
	{
		if (t < 0f)
		{
			t += 1f;
		}

		if (t > 1f)
		{
			t -= 1f;
		}

		if (t < 1f / 6f)
		{
			return p + (q - p) * 6f * t;
		}

		if (t < 1f / 2f)
		{
			return q;
		}

		if (t < 2f / 3f)
		{
			return p + (q - p) * (2f / 3f - t) * 6f;
		}

		return p;
	}
#endif
}
