#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Composition;

public partial class Vector2KeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<Vector2>> _keyFrames = new();

	internal Vector2KeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	public void InsertKeyFrame(float normalizedProgressKey, Vector2 value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value, EasingFunction = easingFunction };

	public void InsertKeyFrame(float normalizedProgressKey, Vector2 value)
		=> InsertKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	public override void InsertExpressionKeyFrame(float normalizedProgressKey, string value)
		=> InsertExpressionKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	public override void InsertExpressionKeyFrame(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = Vector2.Zero, EasingFunction = easingFunction, Expression = value };

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = new()
			{
				Value = (Vector2)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString()),
				EasingFunction = Compositor.GetDefaultEasingFunction()
			};
		}

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		var owner = this;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Vector2 Lerp(AnimationKeyFrame<Vector2> value1, AnimationKeyFrame<Vector2> value2, float amount)
			=> Vector2.Lerp(ResolveVector2KeyFrameValue(owner, value1), ResolveVector2KeyFrameValue(owner, value2), value2.EasingFunction.Ease(amount));

		_keyframeEvaluator = new KeyFrameEvaluator<Vector2>(startValue, finalValue, Duration, _keyFrames, Lerp, IterationCount, IterationBehavior, Compositor);
		return ResolveVector2KeyFrameValue(this, startValue);
	}

	private static Vector2 ResolveVector2KeyFrameValue(KeyFrameAnimation animation, AnimationKeyFrame<Vector2> frame)
	{
		if (frame.Expression is null)
		{
			return frame.Value;
		}

		var parsed = new ExpressionAnimationParser(frame.Expression).Parse();
		var evaluation = parsed.Evaluate(animation);
		return SubPropertyHelpers.ValidateValue<Vector2>(evaluation);
	}
}
