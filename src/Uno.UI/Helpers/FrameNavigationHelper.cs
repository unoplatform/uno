#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

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
		public static PageStackEntry? GetCurrentEntry(Frame? frame) => frame?.GetCurrentPageStackEntry();

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
