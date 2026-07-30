#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Composition;

public partial class Vector3KeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<Vector3>> _keyFrames = new();

	internal Vector3KeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, Vector3 value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value, EasingFunction = easingFunction };

	public void InsertKeyFrame(float normalizedProgressKey, Vector3 value)
		=> InsertKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	private protected override void InsertExpressionKeyFrameCore(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = Vector3.Zero, EasingFunction = easingFunction, ParsedExpression = new ExpressionAnimationParser(value).Parse() };

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = new()
			{
				Value = (Vector3)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString()),
				EasingFunction = Compositor.GetDefaultEasingFunction()
			};
		}

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		// Pass 'this' (the parent animation) so expression keyframes can resolve reference parameters.
		Vector3 Resolve(AnimationKeyFrame<Vector3> frame) => ResolveVector3KeyFrameValue(this, frame);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Vector3 Lerp(AnimationKeyFrame<Vector3> value1, AnimationKeyFrame<Vector3> value2, float amount)
			=> Vector3.Lerp(Resolve(value1), Resolve(value2), value2.EasingFunction.Ease(amount));

		_keyframeEvaluator = new KeyFrameEvaluator<Vector3>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor, Resolve);
		return Resolve(startValue);
	}

	private static Vector3 ResolveVector3KeyFrameValue(KeyFrameAnimation animation, AnimationKeyFrame<Vector3> frame)
	{
		if (frame.ParsedExpression is null)
		{
			return frame.Value;
		}

		var evaluation = frame.ParsedExpression.Evaluate(animation);
		return SubPropertyHelpers.ValidateValue<Vector3>(evaluation);
	}
}
