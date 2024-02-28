#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno;

namespace Microsoft.UI.Composition;

public partial class ScalarKeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, float> _keyFrames = new();

	internal ScalarKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, float value)
		=> _keyFrames[normalizedProgressKey] = value;

	[NotImplemented]
	public void InsertKeyFrame(float normalizedProgressKey, float value, CompositionEasingFunction easingFunction)
		=> InsertKeyFrame(normalizedProgressKey, value);


	internal override object Start()
	{
		base.Start();
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			// TODO: Set startValue to be the current property value.
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
}
