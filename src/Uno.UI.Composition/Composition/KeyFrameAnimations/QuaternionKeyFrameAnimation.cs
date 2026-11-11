#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Composition;

public partial class QuaternionKeyFrameAnimation : KeyFrameAnimation
{
	private readonly SortedDictionary<float, AnimationKeyFrame<Quaternion>> _keyFrames = new();

	internal QuaternionKeyFrameAnimation(Compositor compositor) : base(compositor)
	{
	}

	private protected override int KeyFrameCountCore => _keyFrames.Count;

	internal override CompositionAnimation CloneTemplate()
	{
		var clone = new QuaternionKeyFrameAnimation(Compositor);
		CopyKeyFrames(_keyFrames, clone._keyFrames);
		CopyKeyFrameSettingsTo(clone);
		return clone;
	}

	public void InsertKeyFrame(float normalizedProgressKey, Quaternion value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = value, EasingFunction = easingFunction };

	public void InsertKeyFrame(float normalizedProgressKey, Quaternion value)
		=> InsertKeyFrame(normalizedProgressKey, value, Compositor.GetDefaultEasingFunction());

	private protected override void InsertExpressionKeyFrameCore(float normalizedProgressKey, string value, CompositionEasingFunction easingFunction)
		=> _keyFrames[normalizedProgressKey] = new() { Value = Quaternion.Identity, EasingFunction = easingFunction, ParsedExpression = new ExpressionAnimationParser(value).Parse() };

	internal override object? Start(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, CompositionObject compositionObject)
	{
		base.Start(propertyName, subPropertyName, compositionObject);
		if (!_keyFrames.TryGetValue(0, out var startValue))
		{
			startValue = new()
			{
				Value = (Quaternion)compositionObject.GetAnimatableProperty(propertyName.ToString(), subPropertyName.ToString()),
				EasingFunction = Compositor.GetDefaultEasingFunction()
			};
		}

		if (!_keyFrames.TryGetValue(1.0f, out var finalValue))
		{
			finalValue = _keyFrames.Values.LastOrDefault(startValue);
		}

		// Pass 'this' (the parent animation) so expression keyframes can resolve reference parameters.
		Quaternion Resolve(AnimationKeyFrame<Quaternion> frame) => ResolveQuaternionKeyFrameValue(this, frame);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		Quaternion Lerp(AnimationKeyFrame<Quaternion> value1, AnimationKeyFrame<Quaternion> value2, float amount)
			=> Quaternion.Slerp(Resolve(value1), Resolve(value2), value2.EasingFunction.Ease(amount));

		return StartEvaluator(startValue, finalValue, _keyFrames, Lerp, Resolve);
	}

	private static Quaternion ResolveQuaternionKeyFrameValue(KeyFrameAnimation animation, AnimationKeyFrame<Quaternion> frame)
	{
		if (frame.ParsedExpression is null)
		{
			return frame.Value;
		}

		var evaluation = frame.ParsedExpression.Evaluate(animation);
		return SubPropertyHelpers.ValidateValue<Quaternion>(evaluation);
	}
}
