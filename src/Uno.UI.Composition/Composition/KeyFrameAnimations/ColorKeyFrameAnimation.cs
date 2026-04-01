#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace Microsoft.UI.Composition;

public partial class ColorKeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<Color>> _keyFrames = new();

	internal ColorKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public CompositionColorSpace InterpolationColorSpace { get; set; } = CompositionColorSpace.Rgb;

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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Color Lerp(AnimationKeyFrame<Color> value1, AnimationKeyFrame<Color> value2, float amount)
		{
			float t = value2.EasingFunction.Ease(amount);
			return Color.FromArgb(
				(byte)(value1.Value.A + (value2.Value.A - value1.Value.A) * t),
				(byte)(value1.Value.R + (value2.Value.R - value1.Value.R) * t),
				(byte)(value1.Value.G + (value2.Value.G - value1.Value.G) * t),
				(byte)(value1.Value.B + (value2.Value.B - value1.Value.B) * t));
		}

		_keyframeEvaluator = new KeyFrameEvaluator<Color>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue.Value;
	}
}
