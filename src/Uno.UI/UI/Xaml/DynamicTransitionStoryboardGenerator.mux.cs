// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference DynamicTransitionStoryboardGenerator.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI;

namespace Microsoft.UI.Xaml;

internal static class DynamicTransitionStoryboardGenerator
{
	private static Duration GetDurationFromTransition(VisualTransition transition) => transition.GeneratedDuration;

#if !HAS_UNO
	// TODO Uno: NOT PORTED - HasMatchingDynamicTimeline and GenerateThemeAnimationTransitions (lines 43-99 of
	// DynamicTransitionStoryboardGenerator.cpp): the theme animations aren't dynamic timelines generating their children.
#endif

	private static void SetTargetValues(
		Timeline source,
		Timeline destination)
	{
		// Get both the target name and object-based target from
		// the source timeline first.
		var targetName = Storyboard.GetTargetName(source);
		var target = source.GetTargetObject();
		// todo: the above does not take the TargetObjectWeakRef into account, only manual
		// find out if that is for a reason
		// META-TODO: Figure out what the above TODO actually means...

		// If the source doesn't have either a target OR a target name, we query for its timing parent, which might
		// be a dynamic timeline. They do things differently and we look at their target name and target additionally
		// for clues as to what we're suppose to do here.
		if (target is null && string.IsNullOrEmpty(targetName))
		{
			// TODO Uno: NOT PORTED - the timing parent can't be a CDynamicTimeline.
		}
		else if (target is null)
		{
			// This is inconsistent behavior- If the target name is empty
			// we silently continue, otherwise we fail if the element can't be resolved.
			// Ideally we could rely entirely on the namescope component to validate the target
			// name, and always fail on empty strings.
			if (!string.IsNullOrEmpty(targetName))
			{
				// TODO Uno: WinUI looks the name up from the element of the visual state group (TryGetElementByName).
				target = source.ResolveName(targetName, source.GetTimingParent())
					?? throw new InvalidOperationException($"Cannot resolve TargetName {targetName}.");
			}
		}

		Storyboard.SetTargetName(destination, targetName);
		destination.SetTargetObject(target);

		// Clone the Storyboard.TargetProperty attached property
		Storyboard.SetTargetProperty(destination, Storyboard.GetTargetProperty(source));

		// Clone the target property reference
		destination.SetTargetProperty(source.GetTargetProperty());
	}

	private static Timeline? GenerateToAnimation(Timeline timeline)
	{
		Timeline? newTimeline;

		switch (timeline)
		{
			case DoubleAnimation doubleAnimation when (doubleAnimation.From ?? doubleAnimation.To) is { } previousValue:
				newTimeline = new DoubleAnimation { To = previousValue };
				break;

			case PointAnimation pointAnimation when (pointAnimation.From ?? pointAnimation.To) is { } previousValue:
				newTimeline = new PointAnimation { To = previousValue };
				break;

			case ColorAnimation colorAnimation when (colorAnimation.From ?? colorAnimation.To) is { } previousValue:
				newTimeline = new ColorAnimation { To = previousValue };
				break;

			case DoubleAnimationUsingKeyFrames doubleKeyFrames when doubleKeyFrames.KeyFrames.Count > 0:
				newTimeline = new DoubleAnimation { To = doubleKeyFrames.KeyFrames[0].Value };
				break;

			case PointAnimationUsingKeyFrames pointKeyFrames when pointKeyFrames.KeyFrames.Count > 0:
				newTimeline = new PointAnimation { To = pointKeyFrames.KeyFrames[0].Value };
				break;

			case ColorAnimationUsingKeyFrames colorKeyFrames when colorKeyFrames.KeyFrames.Count > 0:
				newTimeline = new ColorAnimation { To = colorKeyFrames.KeyFrames[0].Value };
				break;

			// If this isn't a property we know how to generate a timeline for,
			// or there isn't any value set, simply skip this item.
			default:
				return null;
		}

		SetTargetValues(timeline, newTimeline);
		return newTimeline;
	}

	private static Timeline? GenerateFromAnimation(Timeline timeline)
	{
		Timeline newTimeline;

		switch (timeline)
		{
			case DoubleAnimation:
			case DoubleAnimationUsingKeyFrames:
				newTimeline = new DoubleAnimation();
				break;

			case PointAnimation:
			case PointAnimationUsingKeyFrames:
				newTimeline = new PointAnimation();
				break;

			case ColorAnimation:
			case ColorAnimationUsingKeyFrames:
				newTimeline = new ColorAnimation();
				break;

			default:
				return null;
		}

		SetTargetValues(timeline, newTimeline);
		return newTimeline;
	}

	private static Timeline? GenerateToAnimation(ResolvedVisualStateSetter setter)
	{
		Timeline newTimeline;

		switch (setter.Value)
		{
			case float value:
				newTimeline = new DoubleAnimation { To = value };
				break;

			case double value:
				newTimeline = new DoubleAnimation { To = value };
				break;

			case Point value:
				newTimeline = new PointAnimation { To = value };
				break;

			case Color value:
				newTimeline = new ColorAnimation { To = value };
				break;

			// If this isn't a property we know how to generate a timeline for
			// we return early.
			default:
				return null;
		}

		newTimeline.SetTargetObject(setter.TargetObject);
		newTimeline.SetTargetProperty(setter.TargetProperty);
		return newTimeline;
	}

	private static Timeline? GenerateFromAnimation(ResolvedVisualStateSetter setter)
	{
		// If we are here, that means that we are transitioning from a state with a setter,
		// to a state where there is no setter for this target object and property. In other
		// words, we are transitioning to the base value, so we will use that as the To value
		// for the visual transition.
		var value = setter.TargetObject.GetBaseValue(setter.TargetProperty).value;

		Timeline newTimeline;

		switch (setter.Value)
		{
			case float or double when value is double baseValue:
				newTimeline = new DoubleAnimation { To = baseValue };
				break;

			case Point when value is Point baseValue:
				newTimeline = new PointAnimation { To = baseValue };
				break;

			case Color when value is Color baseValue:
				newTimeline = new ColorAnimation { To = baseValue };
				break;

			// If this isn't a property we know how to generate a timeline for
			// we return early.
			default:
				return null;
		}

		newTimeline.SetTargetObject(setter.TargetObject);
		newTimeline.SetTargetProperty(setter.TargetProperty);
		return newTimeline;
	}

#if !HAS_UNO
	// TODO Uno: NOT PORTED - ApplyTransformOrginWorkaround (lines 434-501 of DynamicTransitionStoryboardGenerator.cpp):
	// it only applies to TransitionTarget.TransformOrigin, and TransitionTarget is not ported.
#endif

	private static void SetEasingFunction(Timeline timeline, EasingFunctionBase easingFunction)
	{
		switch (timeline)
		{
			case DoubleAnimation doubleAnimation:
				doubleAnimation.EasingFunction = easingFunction;
				break;

			case PointAnimation pointAnimation:
				pointAnimation.EasingFunction = easingFunction;
				break;

			case ColorAnimation colorAnimation:
				colorAnimation.EasingFunction = easingFunction;
				break;

			default:
				global::System.Diagnostics.Debug.Assert(false);
				break;
		}
	}

	internal static Storyboard GenerateDynamicTransitionAnimations(
		VisualTransition transition,
		IEnumerable<Storyboard?> activeStoryboards,
		Storyboard? destinationStoryboard,
		IEnumerable<ResolvedVisualStateSetter> activeSetters,
		IEnumerable<ResolvedVisualStateSetter> destinationSetters)
	{
		var currentAnimations = new TimelineLookupList(activeStoryboards);
		var currentSetters = new TimelineLookupList(activeSetters);
		var transitionAnimations = new TimelineLookupList(transition.Storyboard);
		var newStateAnimations = new TimelineLookupList(destinationStoryboard);
		var newStateSetters = new TimelineLookupList(destinationSetters);
		var duration = GetDurationFromTransition(transition);

		var storyboard = new Storyboard();

		// TODO Uno: NOT PORTED - GenerateThemeAnimationTransitions(newStateAnimations, transitionAnimations, storyboard).

		// We need to respect VisualTransition.GeneratedDuration's constraint, which is
		// that the total duration is the longer of either GeneratedDuration, or the child storybards
		storyboard.Duration = duration;

		foreach (var node in currentSetters)
		{
			currentAnimations.RemoveMatchingNodes(node);
		}

		foreach (var node in transitionAnimations)
		{
			currentAnimations.RemoveMatchingNodes(node);
			currentSetters.RemoveMatchingNodes(node);
			newStateAnimations.RemoveMatchingNodes(node);
			newStateSetters.RemoveMatchingNodes(node);
		}

		foreach (var node in newStateAnimations)
		{
			currentAnimations.RemoveMatchingNodes(node);
			currentSetters.RemoveMatchingNodes(node);
		}

		foreach (var node in newStateSetters)
		{
			currentAnimations.RemoveMatchingNodes(node);
			currentSetters.RemoveMatchingNodes(node);
		}

		void TimelineGenerationFunc(TimelineLookupList.Node node, bool processingCurrentAnimations)
		{
			Timeline? newTimeline;
			if (node.m_setter is { } setter)
			{
				newTimeline = processingCurrentAnimations ? GenerateFromAnimation(setter) : GenerateToAnimation(setter);
			}
			else
			{
				newTimeline = processingCurrentAnimations ? GenerateFromAnimation(node.m_timeline!) : GenerateToAnimation(node.m_timeline!);
			}

			if (newTimeline is not null)
			{
				newTimeline.GetAnimationTargets(out var resolvedTarget, out _);
				global::System.Diagnostics.Debug.Assert(resolvedTarget is not null);

				var shouldCreateWithFullDuration = resolvedTarget is not null && newTimeline.IsIndependentAnimationFor(resolvedTarget);

				// TODO Uno: NOT PORTED - ApplyTransformOrginWorkaround for the current animations.

				newTimeline.Duration = shouldCreateWithFullDuration ? duration : new Duration(TimeSpan.Zero);

				if (transition.GeneratedEasingFunction is { } easingFunction && shouldCreateWithFullDuration)
				{
					SetEasingFunction(newTimeline, easingFunction);
				}

				storyboard.Children.Add(newTimeline);
			}
		}

		foreach (var node in newStateAnimations)
		{
			TimelineGenerationFunc(node, false);
		}

		foreach (var node in newStateSetters)
		{
			TimelineGenerationFunc(node, false);
		}

		foreach (var node in currentAnimations)
		{
			TimelineGenerationFunc(node, true);
		}

		foreach (var node in currentSetters)
		{
			TimelineGenerationFunc(node, true);
		}

		return storyboard;
	}
}
