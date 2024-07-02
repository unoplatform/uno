#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Helpers
{
	/// <summary>
	/// Helper to expose useful properties and methods related to Frame-based navigation logic.
	/// </summary>
	public static class FrameNavigationHelper
	{
		/// <summary>
		/// Returns the <see cref="PageStackEntry"/> for the currently displayed <see cref="Page"/> within the given <paramref name="frame"/>.
		/// </summary>
		/// <param name="frame">The frame used for navigation</param>
		/// /// <returns><see cref="PageStackEntry"/></returns>
		public static PageStackEntry? GetCurrentEntry(Frame? frame) => frame?.CurrentEntry;

		/// <summary>
		/// Returns the actual <see cref="Page"/> instance of the given <paramref name="entry"/>.
		/// </summary>
		/// <param name="entry">The PageStackEntry from the Frame's BackStack.</param>
		/// <returns><see cref="Page"/></returns>
		public static Page? GetInstance(PageStackEntry? entry) => entry?.Instance;

		/// <summary>
		/// Retrieves the current <see cref="Page"/> instance of the given <paramref name="entry"/>. If no instance exists, the <see cref="Page"/> will be created and properly initialized to the provided <paramref name="frame"/>.
		/// </summary>
		/// <param name="entry">The PageStackEntry from the Frame's BackStack.</param>
		/// <returns><see cref="Page"/></returns>
		public static Page? EnsurePageInitialized(Frame? frame, PageStackEntry? entry) => frame?.EnsurePageInitialized(entry);

		/// <summary>
		/// Creates a new instance of <see cref="NavigationEventArgs"/>
		/// </summary>
		/// <param name="content"></param>
		/// <param name="navigationMode"></param>
		/// <param name="navigationTransitionInfo"></param>
		/// <param name="parameter"></param>
		/// <param name="sourcePageType"></param>
		/// <param name="uri"></param>
		/// <returns><see cref="NavigationEventArgs"/></returns>
		public static NavigationEventArgs CreateNavigationEventArgs(
			object? content,
			NavigationMode navigationMode,
			NavigationTransitionInfo? navigationTransitionInfo,
			object? parameter,
			Type sourcePageType,
			Uri? uri
		) => new NavigationEventArgs(content, navigationMode, navigationTransitionInfo, parameter, sourcePageType, uri);
	}
}
