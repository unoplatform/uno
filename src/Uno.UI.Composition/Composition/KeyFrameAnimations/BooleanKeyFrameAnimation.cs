#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.UI.Composition;

public partial class BooleanKeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<bool>> _keyFrames = new();

	internal BooleanKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, bool value)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value };

	private protected override void InsertExpressionKeyFrameCore(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = false, EasingFunction = easingFunction, ParsedExpression = new ExpressionAnimationParser(value).Parse() };

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = new()
			{
				Value = (bool)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString())
			};
		}

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		// Pass 'this' (the parent animation) so expression keyframes can resolve reference parameters.
		bool Resolve(AnimationKeyFrame<bool> frame) => ResolveBooleanKeyFrameValue(this, frame);

		// Boolean keyframes don't interpolate; hold the lower keyframe's value.
		Func<AnimationKeyFrame<bool>, AnimationKeyFrame<bool>, float, bool> lerp = (value1, value2, _) => Resolve(value1);
		_keyframeEvaluator = new KeyFrameEvaluator<bool>(startValue, finalValue, Duration, _keyFrames, lerp, IterationCount, IterationBehavior, Compositor, Resolve);
		return Resolve(startValue);
	}

	private static bool ResolveBooleanKeyFrameValue(KeyFrameAnimation animation, AnimationKeyFrame<bool> frame)
	{
		if (frame.ParsedExpression is null)
		{
			return frame.Value;
		}

		var evaluation = frame.ParsedExpression.Evaluate(animation);
		return SubPropertyHelpers.ValidateValue<bool>(evaluation);
	}
}
