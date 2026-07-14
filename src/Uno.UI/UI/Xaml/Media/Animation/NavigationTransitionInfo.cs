using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{
	public partial class NavigationTransitionInfo : DependencyObject
	{
		private string _navigationState;

		protected NavigationTransitionInfo()
		{
			InitializeBinder();
		}

		protected virtual string GetNavigationStateCore() => _navigationState;

		internal string GetNavigationStateCoreInternal() => GetNavigationStateCore();

		protected virtual void SetNavigationStateCore(string navigationState) => _navigationState = navigationState;

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
