#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Composition;

public partial class Vector4KeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<Vector4>> _keyFrames = new();

	internal Vector4KeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, Vector4 value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value, EasingFunction = easingFunction };

	public void InsertKeyFrame(float normalizedProgressKey, Vector4 value)
		=> InsertKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = new()
			{
				Value = (Vector4)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString()),
				EasingFunction = Compositor.GetDefaultEasingFunction()
			};
		}

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static Vector4 Lerp(AnimationKeyFrame<Vector4> value1, AnimationKeyFrame<Vector4> value2, float amount)
			=> Vector4.Lerp(value1.Value, value2.Value, value2.EasingFunction.Ease(amount));

		_keyframeEvaluator = new KeyFrameEvaluator<Vector4>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor);
		return startValue.Value;
	}
}
