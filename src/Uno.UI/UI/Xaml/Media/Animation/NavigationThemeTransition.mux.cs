// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ThemeTransitions.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media.Animation;

public partial class NavigationThemeTransition
{
	#region DefaultNavigationTransitionInfo DependencyProperty

	/// <summary>
	/// Gets or sets the default navigation transition to be used for the type of navigation
	/// that doesn't specify a transition.
	/// </summary>
	public NavigationTransitionInfo DefaultNavigationTransitionInfo
	{
		get => (NavigationTransitionInfo)GetValue(DefaultNavigationTransitionInfoProperty);
		set => SetValue(DefaultNavigationTransitionInfoProperty, value);
	}

	/// <summary>
	/// Identifies the DefaultNavigationTransitionInfo dependency property.
	/// </summary>
	public static DependencyProperty DefaultNavigationTransitionInfoProperty { get; } =
		DependencyProperty.Register(
			nameof(DefaultNavigationTransitionInfo),
			typeof(NavigationTransitionInfo),
			typeof(NavigationThemeTransition),
			new FrameworkPropertyMetadata(default(NavigationTransitionInfo)));

	#endregion

	// Uno: driven by the layout transition machinery (LayoutTransition.mux.cs), not by the element's Loaded event.
	internal override void AttachToElement(IFrameworkElement element)
	{
	}

	internal override void DetachFromElement(IFrameworkElement element)
	{
	}

	internal override bool ParticipatesInTransition(UIElement element, TransitionTrigger transitionTrigger) =>
		(transitionTrigger == TransitionTrigger.Load) || (transitionTrigger == TransitionTrigger.Unload);

	internal override void CreateStoryboard(
		UIElement element,
		Rect start,
		Rect destination,
		TransitionTrigger transitionTrigger,
		IList<Storyboard> storyboards)
	{
		NavigationTransitionInfo? spDefinition;

		var shouldRun = ((transitionTrigger == TransitionTrigger.Load) || (transitionTrigger == TransitionTrigger.Unload));

		if (!shouldRun)
		{
			return;
		}

		// NTT could be placed on any element: only continue if this is a page.
		if (element is not Page spPage)
		{
			return;
		}

		// Get the NavigationTransitionInfo (from Frame.Navigate) from the Frame.  If we aren't in
		// a frame then there is no navigation, so bail.
		var spFrame = spPage.Frame;
		if (spFrame is null)
		{
			return;
		}

		spFrame.GetNavigationTransitionInfoOverride(out spDefinition, out var isBackNavigation, out var isInitialPage);

		// Check to see if the override was set.
		if (spDefinition is null)
		{
			// We will always run an animation unless it is the forward navigation to the first page.
			shouldRun = (!isInitialPage || isBackNavigation);

			// Check new page for its default.
			NavigationThemeTransition? spNavigationThemeTransition = null;

			// Get the current page (new page) from the Frame.
			// Get the Transitions collection from the Page.
			if (spFrame.Content is UIElement spCurrentPageAsUIElement)
			{
				spNavigationThemeTransition = GetNavigationThemeTransitionFromCollection(spCurrentPageAsUIElement.Transitions);
			}

			if (spNavigationThemeTransition is null)
			{
				// App did not override Page.Transitions, so get the
				// Frame.ContentTransitions.
				spNavigationThemeTransition = GetNavigationThemeTransitionFromCollection(spFrame.ContentTransitions);
			}

			if (spNavigationThemeTransition is not null)
			{
				// We have Navigation Theme Transition so we will run the animation.
				shouldRun = true;

				// See if there is a specific animation being requested.
				spDefinition = spNavigationThemeTransition.DefaultNavigationTransitionInfo;
				if (spDefinition is not null)
				{
					// Set this override definition as the override on the Frame.
					spFrame.SetNavigationTransitionInfoOverride(spDefinition);
				}
			}
		}

		if (shouldRun)
		{
			// If the definition has not been set yet, use the default definition.
			spDefinition ??= new EntranceNavigationTransitionInfo();

			IList<Storyboard>? created = null;
			if (isBackNavigation)
			{
				if (transitionTrigger == TransitionTrigger.Load)
				{
					created = spDefinition.CreateStoryboards(element, NavigationTrigger.BackNavigatingTo);
				}
				else if (transitionTrigger == TransitionTrigger.Unload)
				{
					created = spDefinition.CreateStoryboards(element, NavigationTrigger.BackNavigatingAway);
				}
			}
			else
			{
				if (transitionTrigger == TransitionTrigger.Load)
				{
					created = spDefinition.CreateStoryboards(element, NavigationTrigger.NavigatingTo);
				}
				else if (transitionTrigger == TransitionTrigger.Unload)
				{
					created = spDefinition.CreateStoryboards(element, NavigationTrigger.NavigatingAway);
				}
			}

			if (created is not null)
			{
				foreach (var storyboard in created)
				{
					storyboards.Add(storyboard);
				}
			}
		}

		// TODO Uno: prune the element lists once the info types expose their Clear methods (ThemeTransitions.cpp:263-266).
	}

	private static NavigationThemeTransition? GetNavigationThemeTransitionFromCollection(TransitionCollection? transitions)
	{
		NavigationThemeTransition? themeTransition = null;

		// Get the size of that collection (if we have one)
		var numTransitions = transitions?.Count ?? 0;

		// Iterate through the collection to find an NTT.
		for (var k = numTransitions; themeTransition is null && k > 0; k--)
		{
			themeTransition = transitions![k - 1] as NavigationThemeTransition;
		}

		return themeTransition;
	}
}
