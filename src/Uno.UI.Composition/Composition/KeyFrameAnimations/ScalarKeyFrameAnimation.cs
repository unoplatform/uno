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
#if !__APPLE_UIKIT__
	private readonly SortedDictionary<float, AnimationKeyFrame<float>> _keyFrames = new();
#endif

	internal ScalarKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

#if !__APPLE_UIKIT__
	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, float value)
		=> InsertKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	public void InsertKeyFrame(float normalizedProgressKey, float value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value, EasingFunction = easingFunction };

	public override void InsertExpressionKeyFrame(float normalizedProgressKey, string value)
		=> InsertExpressionKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	public override void InsertExpressionKeyFrame(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = 0f, EasingFunction = easingFunction, Expression = value };

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

		// 'this' is the parent animation; we need it so expression keyframes can resolve reference parameters.
		var owner = this;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float Lerp(AnimationKeyFrame<float> value1, AnimationKeyFrame<float> value2, float amount)
			=> float.Lerp(ResolveScalarKeyFrameValue(owner, value1), ResolveScalarKeyFrameValue(owner, value2), value2.EasingFunction.Ease(amount));

		_keyframeEvaluator = new KeyFrameEvaluator<float>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor);
		return ResolveScalarKeyFrameValue(this, startValue);
	}

	private static float ResolveScalarKeyFrameValue(KeyFrameAnimation animation, AnimationKeyFrame<float> frame)
	{
		if (frame.Expression is null)
		{
			return frame.Value;
		}

		var parsed = new ExpressionAnimationParser(frame.Expression).Parse();
		var evaluation = parsed.Evaluate(animation);
		return SubPropertyHelpers.ValidateValue<float>(evaluation);
	}
#endif
}
