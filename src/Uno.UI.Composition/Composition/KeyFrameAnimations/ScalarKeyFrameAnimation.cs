#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno;

namespace Windows.UI.Composition;

public partial class ScalarKeyFrameAnimation : KeyFrameAnimation
{
#if !__IOS__
	private readonly SortedDictionary<float, float> _keyFrames = new();
#endif

	internal ScalarKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

#if !__IOS__
	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, float value)
		=> _keyFrames[normalizedProgressKey] = value;

	[NotImplemented]
	public void InsertKeyFrame(float normalizedProgressKey, float value, CompositionEasingFunction easingFunction)
		=> InsertKeyFrame(normalizedProgressKey, value);


	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = (float)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString());
		}

		Func<float, float, float, float> lerp =
#if NET8_0_OR_GREATER
			float.Lerp;
#else
			(value1, value2, amount) => (value1 * (1.0f - amount)) + (value2 * amount);
#endif

		_keyframeEvaluator = new KeyFrameEvaluator<float>(startValue, _keyFrames[1.0f], Duration, _keyFrames, lerp, IterationCount, IterationBehavior, Compositor);
		return startValue;
	}
#endif
}
