// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference LayoutTransition_partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System.Collections.Generic;
using DirectUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class ContentThemeTransition : global::Microsoft.UI.Xaml.Media.Animation.Transition
{
	public ContentThemeTransition()
	{
	}

	internal override bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger) => true;

	internal override void CreateStoryboard(
		UIElement element,
		Rect start,
		Rect destination,
		TransitionTrigger transitionTrigger,
		IList<Storyboard> storyboards,
		out TransitionParent parentForTransition)
	{
		parentForTransition = TransitionParent.ParentToCommonParent;

		Point startLocation = new(start.X - destination.X, start.Y - destination.Y);
		Point destinationLocation = new(0, 0);
		Point destinationLocationOffset = new((float)HorizontalOffset, (float)VerticalOffset);

		var shouldRun = true;

		GetTransitionContext(element, out var didCheck, out var context);
		if (didCheck)
		{
			shouldRun &= context == ThemeTransitionContext.ContentTransition;
		}

		if (!shouldRun)
		{
			return;
		}

		Storyboard spPrimarySB = new();
		storyboards.Add(spPrimarySB);

		// TODO Uno: on a Layout/Reparent trigger WinUI fades the old location out while the new one fades in, which takes
		// two layout transition elements. Uno has one rendering of the element, so that trigger only plays the incoming part.
		if (transitionTrigger == TransitionTrigger.Unload)
		{
			ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_TRANSITIONCONTENT, ThemeGenerator.TA_TRANSITIONCONTENT_OUTGOING, element, false, startLocation, startLocation, 0, spPrimarySB.Children);
		}
		else
		{
			ThemeGenerator.AddTimelinesForThemeAnimation(ThemeGenerator.TAS_TRANSITIONCONTENT, ThemeGenerator.TA_TRANSITIONCONTENT_INCOMING, element, false, destinationLocationOffset, destinationLocation, 0, spPrimarySB.Children);
		}

		SetTransitionParentForStickyHeaders(element, ref parentForTransition);
	}
}
