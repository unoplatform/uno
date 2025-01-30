#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno;

namespace Microsoft.UI.Composition;

public partial class ScalarKeyFrameAnimation : KeyFrameAnimation
{
#if !__IOS__
	private readonly SortedDictionary<float, AnimationKeyFrame<float>> _keyFrames = new();
#endif

	internal ScalarKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

#if !__IOS__
	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, float value)
		=> InsertKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	public void InsertKeyFrame(float normalizedProgressKey, float value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value, EasingFunction = easingFunction };


	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);

		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = new()
			{
				Value = (float)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString()),
				EasingFunction = Compositor.GetDefaultEasingFunction()
			};
		}

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static float Lerp(AnimationKeyFrame<float> value1, AnimationKeyFrame<float> value2, float amount)
			=> float.Lerp(value1.Value, value2.Value, value2.EasingFunction.Ease(amount));

		_keyframeEvaluator = new KeyFrameEvaluator<float>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue.Value;
	}
#endif
}
