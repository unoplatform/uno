// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference NavigationTransitionInfo_Partial.h, tag winui3/release/2.5.1, commit ba3a8d59e

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class NavigationTransitionInfo : DependencyObject
	{
		protected NavigationTransitionInfo()
		{
			InitializeBinder();
		}

		/// <summary>
		/// When overridden in a derived class, gets the string that is stored in the navigation history for this transition.
		/// </summary>
		/// <returns>The navigation state string.</returns>
		protected virtual string GetNavigationStateCore() => null;

		internal string GetNavigationStateCoreInternal() => GetNavigationStateCore();

		/// <summary>
		/// When overridden in a derived class, restores this transition from a string read from the navigation history.
		/// </summary>
		/// <param name="navigationState">The navigation state string.</param>
		protected virtual void SetNavigationStateCore(string navigationState)
		{
		}

		internal void SetNavigationStateCoreInternal(string navigationState) => SetNavigationStateCore(navigationState);

		/// <summary>
		/// Creates the storyboards for the navigation transition animation.
		/// </summary>
		/// <param name="element">The element to animate.</param>
		/// <param name="trigger">The navigation trigger indicating the type of navigation.</param>
		/// <returns>A list of storyboards to run for the transition.</returns>
		internal IList<Storyboard> CreateStoryboards(UIElement element, NavigationTrigger trigger)
			=> CreateStoryboardsCore(element, trigger);

		/// <summary>
		/// Override this method in derived classes to create the actual transition storyboards.
		/// </summary>
		/// <param name="element">The element to animate.</param>
		/// <param name="trigger">The navigation trigger indicating the type of navigation.</param>
		/// <returns>A list of storyboards to run for the transition.</returns>
		/// <remarks>
		/// WinUI targets a dedicated per-element TransitionTarget layer
		/// ("(UIElement.TransitionTarget).(TransitionTarget.CompositeTransform).TranslateY", "(UIElement.TransitionTarget).Opacity"),
		/// which leaves the app's own RenderTransform and Opacity untouched. Uno has no TransitionTarget, so the
		/// built-in implementations animate UIElement.RenderTransform / UIElement.Opacity directly and therefore
		/// take ownership of those values for the duration of the navigation.
		/// TODO Uno: add a TransitionTarget equivalent so navigation transitions no longer overwrite app-declared
		/// transforms and cannot leave stale values behind on cached pages.
		/// </remarks>
		private protected virtual IList<Storyboard> CreateStoryboardsCore(UIElement element, NavigationTrigger trigger)
			=> new List<Storyboard>();
	}
}
