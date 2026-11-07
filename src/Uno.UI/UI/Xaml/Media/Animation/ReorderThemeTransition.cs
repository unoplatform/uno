// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference LayoutTransition_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System.Collections.Generic;
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class ReorderThemeTransition : global::Microsoft.UI.Xaml.Media.Animation.Transition
{
	public ReorderThemeTransition()
	{
	}

	private static bool IsReorderContext(ThemeTransitionContext context) =>
		context is ThemeTransitionContext.ReorderedItem or
		ThemeTransitionContext.SingleReorderList or
		ThemeTransitionContext.SingleReorderGrid or
		ThemeTransitionContext.MultipleReorderList or
		ThemeTransitionContext.MultipleReorderGrid;

	internal override bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger)
	{
		var shouldRun = transitionTrigger is TransitionTrigger.Layout or TransitionTrigger.Load or TransitionTrigger.Reparent;

		if (shouldRun)
		{
			GetTransitionContext(element, out var didCheck, out var context);
			if (didCheck)
			{
				shouldRun = IsReorderContext(context);
			}
		}

		return shouldRun;
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

		var shouldRun = transitionTrigger is TransitionTrigger.Layout or TransitionTrigger.Load or TransitionTrigger.Reparent;

		if (shouldRun)
		{
			GetTransitionContext(element, out var didCheck, out var context);
			if (didCheck)
			{
				shouldRun &= IsReorderContext(context);
			}
		}

		if (shouldRun)
		{
			AddDeleteRepositionStoryboardCreator(element, start, destination, transitionTrigger, storyboards, out parentForTransition);
		}
	}
}
