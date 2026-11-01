// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference NavigateTransitionHelper.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Helpers.Boxes;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

internal static partial class NavigateTransitionHelper
{
	internal static void AddTimelineToStoryboard(
		Timeline timeline,
		Storyboard storyboard)
	{
		// Get timelines from storyboard and add timeline.
		storyboard.Children.Add(timeline);
	}

	internal static Storyboard CreateObjectStoryboardForNavigationAnimations(
		List<ObjectAnimationDescription> animations)
	{
		// Make storyboard.
		Storyboard spStoryboard = new();

		// Get timelines from storyboard and add timeline.
		var spTimelines = spStoryboard.Children;

		foreach (var it in animations)
		{
			var spAnimation = GetObjectAnimation(it);

			spTimelines.Add(spAnimation);
		}

		return spStoryboard;
	}

	internal static ObjectAnimationUsingKeyFrames GetObjectAnimation(
		ObjectAnimationDescription desc)
	{
		ObjectAnimationUsingKeyFrames spAnimation = new();

		// Set targets for the timeline.
		Storyboard.SetTarget(spAnimation, desc.m_element!);
		Storyboard.SetTargetProperty(spAnimation, desc.m_prop!);

		// Get key frames from animation and add key frames.
		var spKeyframeCollection = spAnimation.KeyFrames;

		foreach (var it in desc.m_pKeyFrameDescs!)
		{
			var spKeyFrame = GetObjectKeyFrame(it);

			spKeyframeCollection.Add(spKeyFrame);
		}

		return spAnimation;
	}

	internal static ObjectKeyFrame GetObjectKeyFrame(
		ObjectKeyFrameDescription desc)
	{
		const long ticksPerMillisecond = 10000;

		// Make keyframe.
		DiscreteObjectKeyFrame spKeyFrame = new();

		var spValueInspectable = desc.m_value ? BoolBoxes.True : BoolBoxes.False;

		// Add the key time and key value to the key frame.
		spKeyFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(desc.m_time * ticksPerMillisecond));
		spKeyFrame.Value = spValueInspectable;

		return spKeyFrame;
	}

	internal static Storyboard CreateStoryboardForNavigationAnimations(
		List<DoubleAnimationDescription> animations)
	{
		// Make storyboard.
		Storyboard spStoryboard = new();

		// Get timelines from storyboard and add timeline.
		var spTimelines = spStoryboard.Children;

		foreach (var it in animations)
		{
			var spAnimation = GetAnimation(it);

			spTimelines.Add(spAnimation);
		}

		return spStoryboard;
	}

	internal static DoubleAnimationUsingKeyFrames GetAnimation(
		DoubleAnimationDescription desc)
	{
		DoubleAnimationUsingKeyFrames spAnimation = new();

		// Set targets for the timeline.
		Storyboard.SetTarget(spAnimation, desc.m_element!);
		Storyboard.SetTargetProperty(spAnimation, desc.m_prop!);

		// Get key frames from animation and add key frames.
		var spKeyframeCollection = spAnimation.KeyFrames;

		foreach (var it in desc.m_pKeyFrameDescs!)
		{
			var spKeyFrame = GetDoubleKeyFrame(it);

			spKeyframeCollection.Add(spKeyFrame);
		}

		return spAnimation;
	}

	internal static DoubleKeyFrame GetDoubleKeyFrame(
		DoubleKeyFrameDescription desc)
	{
		const long ticksPerMillisecond = 10000;

		DoubleKeyFrame spKeyFrame;

		// Make keyframe.
		if (desc.m_easingFunction is not null)
		{
			EasingDoubleKeyFrame spEasingKeyFrame = new();
			spEasingKeyFrame.EasingFunction = desc.m_easingFunction;
			spKeyFrame = spEasingKeyFrame;
		}
		else
		{
			spKeyFrame = new LinearDoubleKeyFrame();
		}

		// Add the key time and key value to the key frame.
		spKeyFrame.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(desc.m_time * ticksPerMillisecond));
		spKeyFrame.Value = desc.m_value;

		return spKeyFrame;
	}

	internal static EasingFunctionBase GetExponentialEase(
		double exponent,
		EasingMode mode)
	{
		ExponentialEase spExponentialEase = new();
		spExponentialEase.Exponent = exponent;

		spExponentialEase.EasingMode = mode;

		return spExponentialEase;
	}

	internal static EasingFunctionBase GetPowerEase(
		double power,
		EasingMode mode)
	{
		PowerEase spPowerEase = new();
		spPowerEase.Power = power;

		spPowerEase.EasingMode = mode;

		return spPowerEase;
	}

	internal static EasingFunctionBase GetCircleEase(
		EasingMode mode)
	{
		CircleEase spCircleEase = new();

		spCircleEase.EasingMode = mode;

		return spCircleEase;
	}

	internal static bool IsAncestor(
		FrameworkElement ancestor,
		FrameworkElement possibleChild)
	{
		var spParentOfChildAsDO = VisualTreeHelper.GetParent(possibleChild);

		if (spParentOfChildAsDO is null)
		{
			// The parent is null, so return false (already set).
			return false;
		}

		// UNO TODO: WinUI fails with E_NOINTERFACE when the parent is not a FrameworkElement.
		if (spParentOfChildAsDO is not FrameworkElement spParentOfChildAsFrameworkElement)
		{
			return false;
		}

		if (spParentOfChildAsFrameworkElement == ancestor)
		{
			return true;
		}
		else
		{
			return IsAncestor(ancestor, spParentOfChildAsFrameworkElement);
		}
	}

	internal static void SetTransformOrigin(
		UIElement element,
		Point origin,
		Storyboard storyboard)
	{
		// UNO TODO: WinUI animates "(UIElement.TransitionTarget).TransformOrigin" with a zero-duration PointAnimation.
		// Uno has neither TransitionTarget nor PointAnimation, so a discrete key frame at t=0 animates
		// RenderTransformOrigin instead; like WinUI, the value only lives as long as the storyboard.
		ObjectAnimationUsingKeyFrames spTransformOriginAnimation = new();

		// Set the target and target property.
		Storyboard.SetTarget(spTransformOriginAnimation, element);
		Storyboard.SetTargetProperty(spTransformOriginAnimation, nameof(UIElement.RenderTransformOrigin));

		// Set destination value.
		spTransformOriginAnimation.KeyFrames.Add(new DiscreteObjectKeyFrame
		{
			KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
			Value = origin,
		});

		// Set the duration of the animation.
		spTransformOriginAnimation.Duration = new Duration(TimeSpan.Zero);

		AddTimelineToStoryboard(spTransformOriginAnimation, storyboard);
	}

	internal static void RegisterSplineKeyFrame(
		DoubleAnimationUsingKeyFrames animation,
		double value,
		long keyTimeInMilliseconds,
		Point controlPoint1,
		Point controlPoint2)
	{
		const long millisecondsTo100Nanoseconds = 10000; // 10^6 / 100

		KeySpline keySpline = new();
		SplineDoubleKeyFrame splineDoubleKeyFrame = new();

		var keyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(keyTimeInMilliseconds * millisecondsTo100Nanoseconds));
		keySpline.ControlPoint1 = controlPoint1;
		keySpline.ControlPoint2 = controlPoint2;

		splineDoubleKeyFrame.KeySpline = keySpline;
		splineDoubleKeyFrame.KeyTime = keyTime;
		splineDoubleKeyFrame.Value = value;

		animation.KeyFrames.Add(splineDoubleKeyFrame);
	}

	internal static void RegisterEasingKeyFrame(
		DoubleAnimationUsingKeyFrames animation,
		double value,
		long keyTimeInMilliseconds,
		EasingFunctionBase easingFunction)
	{
		const long millisecondsTo100Nanoseconds = 10000; // 10^6 / 100

		EasingDoubleKeyFrame easingDoubleKeyFrame = new();

		easingDoubleKeyFrame.EasingFunction = easingFunction;

		var keyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(keyTimeInMilliseconds * millisecondsTo100Nanoseconds));

		easingDoubleKeyFrame.KeyTime = keyTime;
		easingDoubleKeyFrame.Value = value;

		animation.KeyFrames.Add(easingDoubleKeyFrame);
	}

	internal static void RegisterDiscreteKeyFrame(
		DoubleAnimationUsingKeyFrames animation,
		double value,
		long keyTimeInMilliseconds)
	{
		const long millisecondsTo100Nanoseconds = 10000; // 10^6 / 100

		DiscreteDoubleKeyFrame discreteDoubleKeyFrame = new();

		var keyTime = KeyTime.FromTimeSpan(TimeSpan.FromTicks(keyTimeInMilliseconds * millisecondsTo100Nanoseconds));

		discreteDoubleKeyFrame.KeyTime = keyTime;
		discreteDoubleKeyFrame.Value = value;

		animation.KeyFrames.Add(discreteDoubleKeyFrame);
	}

	internal static void RemoveHitTestVisbility(
		IList<Timeline> timelines,
		DependencyObject element)
	{
		// UNO TODO: WinUI uses the "UIElement.IsHitTestVisible" path; Uno resolves the plain property name.
		var propertyNameIsHitTestVisible = nameof(UIElement.IsHitTestVisible);

		ObjectAnimationUsingKeyFrames objectAnimationUsingKeyFrames = new();

		DiscreteObjectKeyFrame discreteObjectKeyFrame = new();

		var keyTime = KeyTime.FromTimeSpan(TimeSpan.Zero);

		discreteObjectKeyFrame.KeyTime = keyTime;
		discreteObjectKeyFrame.Value = BoolBoxes.False;

		objectAnimationUsingKeyFrames.KeyFrames.Add(discreteObjectKeyFrame);

		Storyboard.SetTarget(objectAnimationUsingKeyFrames, element);
		Storyboard.SetTargetProperty(objectAnimationUsingKeyFrames, propertyNameIsHitTestVisible);

		timelines.Add(objectAnimationUsingKeyFrames);
	}
}
