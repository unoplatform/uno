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
		var transitionInfo = GetEffectiveNavigationTransitionInfo(definitionOverride, page, frame, out var shouldRun);

		// Skip animation for forward navigation to the first page (unless there's an explicit override)
		if (!shouldRun && isInitialPage && !isBackNavigation && definitionOverride is null)
		{
			return;
		}

		if (transitionInfo is null)
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
		out bool shouldRun)
	{
		shouldRun = true;

		// Priority 1: Use the override from Frame.Navigate()
		if (definitionOverride is not null)
		{
			return definitionOverride;
		}

		// Priority 2: Check Page.Transitions for NavigationThemeTransition
		var pageTransition = GetNavigationThemeTransitionFromCollection(page.Transitions);
		if (pageTransition?.DefaultNavigationTransitionInfo is not null)
		{
			// Set the override on Frame for consistency
			frame.SetNavigationTransitionInfoOverride(pageTransition.DefaultNavigationTransitionInfo);
			return pageTransition.DefaultNavigationTransitionInfo;
		}

		// Priority 3: Check Frame.ContentTransitions for NavigationThemeTransition
		var frameTransition = GetNavigationThemeTransitionFromCollection(frame.ContentTransitions);
		if (frameTransition?.DefaultNavigationTransitionInfo is not null)
		{
			return frameTransition.DefaultNavigationTransitionInfo;
		}

		// Priority 4: Use this transition's DefaultNavigationTransitionInfo
		if (DefaultNavigationTransitionInfo is not null)
		{
			return DefaultNavigationTransitionInfo;
		}

		// Priority 5: Fallback to EntranceNavigationTransitionInfo
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
