#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace Windows.UI.Composition;

public partial class Vector4KeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, Vector4> _keyFrames = new();

	internal Vector4KeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, Vector4 value)
		=> _keyFrames[normalizedProgressKey] = value;

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = (Vector4)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString());
		}

		_keyframeEvaluator = new KeyFrameEvaluator<Vector4>(startValue, _keyFrames[1.0f], Duration, _keyFrames, Vector4.Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue;
	}
}
