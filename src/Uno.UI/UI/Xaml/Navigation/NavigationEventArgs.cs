using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Navigation;

/// <summary>
/// Provides data for navigation methods and event handlers that cannot cancel the navigation request.
/// </summary>
public sealed partial class NavigationEventArgs
{
	internal NavigationEventArgs(
		object content,
		NavigationMode navigationMode,
		NavigationTransitionInfo navigationTransitionInfo,
		object parameter,
		Type sourcePageType,
		Uri uri)
	{
		Content = content;
		NavigationMode = navigationMode;
		NavigationTransitionInfo = navigationTransitionInfo;
		Parameter = parameter;
		SourcePageType = sourcePageType;
		Uri = uri;
	}

	/// <summary>
	/// Gets the root node of the target page's content.
	/// </summary>
	public object Content { get; }

	/// <summary>
	/// Gets a value that indicates the direction of movement during navigation
	/// </summary>
	public NavigationMode NavigationMode { get; }

	/// <summary>
	/// Gets a value that indicates the animated transition associated with the navigation.
	/// </summary>
	public NavigationTransitionInfo NavigationTransitionInfo { get; }

	/// <summary>
	/// Gets any "Parameter" object passed to the target page for the navigation.
	/// </summary>
	public object Parameter { get; }

	/// <summary>
	/// Gets the value of the sourcePageType parameter (the page being navigated to) from the originating Navigate call.
	/// </summary>
	public Type SourcePageType { get; }

	/// <summary>
	/// Gets the Uniform Resource Identifier (URI) of the target.
	/// </summary>
	public Uri Uri { get; set; }
}
