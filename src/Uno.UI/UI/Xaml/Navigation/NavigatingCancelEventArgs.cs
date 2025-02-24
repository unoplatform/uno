using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Media.Animation;

namespace Windows.UI.Xaml.Navigation;

/// <summary>
/// Provides data for the Page.OnNavigatingFrom callback that can be used to cancel a navigation request from origination.
/// </summary>
public sealed partial class NavigatingCancelEventArgs
{
	public NavigatingCancelEventArgs(NavigationMode navigationMode, NavigationTransitionInfo navigationTransitionInfo, object parameter, Type sourcePageType)
	{
		NavigationMode = navigationMode;
		NavigationTransitionInfo = navigationTransitionInfo;
		Parameter = parameter;
		SourcePageType = sourcePageType;
	}

	/// <summary>
	/// Specifies whether a pending navigation should be canceled.
	/// </summary>
	public bool Cancel { get; set; }

	/// <summary>
	/// Gets the value of the mode parameter from the originating Navigate call.
	/// </summary>
	public NavigationMode NavigationMode { get; }

	/// <summary>
	/// Gets a value that indicates the animated transition associated with the navigation.
	/// </summary>
	public NavigationTransitionInfo NavigationTransitionInfo { get; }

	/// <summary>
	/// Gets the navigation parameter associated with this navigation.
	/// </summary>
	public object Parameter { get; }

	/// <summary>
	/// Gets the value of the sourcePageType parameter (the page being navigated to) from the originating Navigate call.
	/// </summary>
	public Type SourcePageType { get; }
}
