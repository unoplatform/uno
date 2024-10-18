#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Windows.UI.Composition;

public partial class Vector2KeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, Vector2> _keyFrames = new();

	internal Vector2KeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, Vector2 value)
		=> _keyFrames[normalizedProgressKey] = value;

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = (Vector2)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString());
		}

		_keyframeEvaluator = new KeyFrameEvaluator<Vector2>(startValue, _keyFrames[1.0f], Duration, _keyFrames, Vector2.Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue;
	}
}
