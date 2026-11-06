// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference LayoutTransition_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System.Collections.Generic;
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class AddDeleteThemeTransition : global::Microsoft.UI.Xaml.Media.Animation.Transition
{
	public AddDeleteThemeTransition()
	{
	}

	internal override bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger)
	{
		// we do not know how to properly express animations with lots of interruptions. We do not have any independent handoff
		// for transitions and they jump back to hold their location (as known to the uithread). This looks very bad, so
		// we fall back to no animations.
		// Note how i'm doing this both for grids and non grid layouts, since even in list scenarios, this looks very jarring.
		return !GetSpeedOfChanges(element);
	}

	internal override void CreateStoryboard(
		UIElement element,
		Rect start,
		Rect destination,
		TransitionTrigger transitionTrigger,
		IList<Storyboard> storyboards,
		out TransitionParent parentForTransition)
	{
		parentForTransition = TransitionParent.ParentToCommonParent;

		// we do not know how to properly express animations with lots of interruptions. We do not have any independent handoff
		// for transitions and they jump back to hold their location (as known to the uithread). This looks very bad, so
		// we fall back to no animations.
		// Note how i'm doing this both for grids and non grid layouts, since even in list scenarios, this looks very jarring.
		var shouldRun = !GetSpeedOfChanges(element);
		var didCheck = false;
		var context = ThemeTransitionContext.None;

		if (shouldRun)
		{
			GetTransitionContext(element, out didCheck, out context);
		}
		if (didCheck)
		{
			shouldRun &=
				context is ThemeTransitionContext.SingleAddList or
				ThemeTransitionContext.SingleDeleteList or
				ThemeTransitionContext.SingleAddGrid or
				ThemeTransitionContext.SingleDeleteGrid or
				ThemeTransitionContext.MixedOperationsList or
				ThemeTransitionContext.MixedOperationsGrid or
				ThemeTransitionContext.MultipleAddList or
				ThemeTransitionContext.MultipleDeleteList or
				ThemeTransitionContext.MultipleAddGrid or
				ThemeTransitionContext.MultipleDeleteGrid;
		}

		if (shouldRun)
		{
			AddDeleteRepositionStoryboardCreator(element, start, destination, transitionTrigger, storyboards, out parentForTransition);
		}

		SetTransitionParentForStickyHeaders(element, ref parentForTransition);
	}
}
