using System.Collections.Generic;
using System.Numerics;

namespace Microsoft.UI.Composition;

public partial class Vector2KeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, Vector2> _keyFrames = new();

	internal Vector2KeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, Vector2 value)
		=> _keyFrames[normalizedProgressKey] = value;

	internal override object Start()
	{
		base.Start();
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			// TODO: Set startValue to be the current property value.
		}

		_keyframeEvaluator = new KeyFrameEvaluator<Vector2>(startValue, _keyFrames[1.0f], Duration, _keyFrames, Vector2.Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue;
	}
}
