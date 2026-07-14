// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ThemeTransitions.cpp, tag winui3/release/1.7-stable

using Microsoft.UI.Xaml.Controls;

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

	/// <summary>
	/// Attaches the transition to a Page element.
	/// </summary>
	internal override void AttachToElement(IFrameworkElement element)
	{
		// NavigationThemeTransition only works with Page elements
		if (element is Page page)
		{
			page.Loaded += OnPageLoaded;
			page.Unloaded += OnPageUnloaded;

			// If the page is already loaded, run the enter animation immediately
			if (page.IsLoaded)
			{
				RunNavigationAnimation(page, isEntering: true);
			}
		}
	}

	/// <summary>
	/// Detaches the transition from a Page element.
	/// </summary>
	internal override void DetachFromElement(IFrameworkElement element)
	{
		if (element is Page page)
		{
			page.Loaded -= OnPageLoaded;
			page.Unloaded -= OnPageUnloaded;
		}
	}

	private void OnPageLoaded(object sender, RoutedEventArgs e)
	{
		if (sender is not Page page)
		{
			return;
		}

		RunNavigationAnimation(page, isEntering: true);
	}

	private void OnPageUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is not Page page)
		{
			return;
		}

		RunNavigationAnimation(page, isEntering: false);
	}

	private void RunNavigationAnimation(Page page, bool isEntering)
	{
		// Get the Frame from the Page
		var frame = page.Frame;
		if (frame is null)
		{
			return;
		}

		// Get navigation state from Frame
		frame.GetNavigationTransitionInfoOverride(
			out var definitionOverride,
			out var isBackNavigation,
			out var isInitialPage);

		// Get the NavigationTransitionInfo to use
		var transitionInfo = GetEffectiveNavigationTransitionInfo(
			definitionOverride,
			page,
			frame,
			isBackNavigation,
			isInitialPage,
			out var shouldRun);

		if (!shouldRun || transitionInfo is null)
		{
			return;
		}

		// Determine the navigation trigger based on direction and enter/exit state
		var trigger = GetNavigationTrigger(isBackNavigation, isEntering);

		// Create and run the storyboards
		var storyboards = transitionInfo.CreateStoryboards(page, trigger);

		foreach (var storyboard in storyboards)
		{
			storyboard.Begin();
		}
	}

	private NavigationTransitionInfo GetEffectiveNavigationTransitionInfo(
		NavigationTransitionInfo definitionOverride,
		Page page,
		Frame frame,
		bool isBackNavigation,
		bool isInitialPage,
		out bool shouldRun)
	{
		// Priority 1: an override was passed in through Frame.Navigate(), use this definition.
		if (definitionOverride is not null)
		{
			shouldRun = true;
			return definitionOverride;
		}

		// We will always run an animation unless it is the forward navigation to the first page.
		shouldRun = !isInitialPage || isBackNavigation;

		// Priority 2: check the page for its default, then the Frame's ContentTransitions.
		var themeTransition =
			GetNavigationThemeTransitionFromCollection(page.Transitions)
			?? GetNavigationThemeTransitionFromCollection(frame.ContentTransitions);

		if (themeTransition is not null)
		{
			// We have a NavigationThemeTransition so we will run the animation.
			shouldRun = true;

			// See if there is a specific animation being requested.
			if (themeTransition.DefaultNavigationTransitionInfo is { } transitionInfo)
			{
				// Set this override definition as the override on the Frame.
				frame.SetNavigationTransitionInfoOverride(transitionInfo);
				return transitionInfo;
			}
		}

		// Priority 3: use this transition's DefaultNavigationTransitionInfo, for the case where it
		// was attached without being reachable through either transition collection.
		if (DefaultNavigationTransitionInfo is not null)
		{
			return DefaultNavigationTransitionInfo;
		}

		// Priority 4: fall back to EntranceNavigationTransitionInfo.
		return new EntranceNavigationTransitionInfo();
	}

	private static NavigationThemeTransition GetNavigationThemeTransitionFromCollection(TransitionCollection transitions)
	{
		if (transitions is null)
		{
			return null;
		}

		// Search in reverse order (last one wins)
		for (int i = transitions.Count - 1; i >= 0; i--)
		{
			if (transitions[i] is NavigationThemeTransition ntt)
			{
				return ntt;
			}
		}

		return null;
	}

	private static NavigationTrigger GetNavigationTrigger(bool isBackNavigation, bool isEntering)
	{
		if (isBackNavigation)
		{
			return isEntering
				? NavigationTrigger.BackNavigatingTo
				: NavigationTrigger.BackNavigatingAway;
		}
		else
		{
			return isEntering
				? NavigationTrigger.NavigatingTo
				: NavigationTrigger.NavigatingAway;
		}
	}
}
