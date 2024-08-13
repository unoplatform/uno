#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Uno;

namespace Microsoft.UI.Composition;

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

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		_keyframeEvaluator = new KeyFrameEvaluator<float>(startValue, finalValue, Duration, _keyFrames, float.Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue;
	}
#endif
}
