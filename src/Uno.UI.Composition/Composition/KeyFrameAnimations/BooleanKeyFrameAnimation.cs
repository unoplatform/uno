using System;
using System.Collections.Generic;

namespace Microsoft.UI.Composition;

public partial class BooleanKeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, bool> _keyFrames = new();

	internal BooleanKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, bool value)
		=> _keyFrames[normalizedProgressKey] = value;

	internal override object Start()
	{
		base.Start();
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			// TODO: Set startValue to be the current property value.
		}

		Func<bool, bool, float, bool> lerp = (value1, value2, _) => value1;
		_keyframeEvaluator = new KeyFrameEvaluator<bool>(startValue, _keyFrames[1.0f], Duration, _keyFrames, lerp, IterationCount, IterationBehavior, Compositor);
		return startValue;
	}
}
