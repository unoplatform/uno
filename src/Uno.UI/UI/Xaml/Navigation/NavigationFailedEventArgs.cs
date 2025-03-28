using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Navigation;

/// <summary>
/// Provides event data for the WebView.NavigationFailed and Frame.NavigationFailed events.
/// </summary>
public sealed partial class NavigationFailedEventArgs
{
	internal NavigationFailedEventArgs(Type sourcePageType, Exception exception)
	{
		SourcePageType = sourcePageType;
		Exception = exception;
	}

	/// <summary>
	/// Gets the result code for the exception that is associated with the failed navigation.
	/// </summary>
	public Exception Exception { get; }

	/// <summary>
	/// Gets or sets a value that indicates whether the failure event has been handled.
	/// </summary>
	public bool Handled { get; set; }

	/// <summary>
	/// Gets the value of the sourcePageType parameter (the page being navigated to) from the originating Navigate call.
	/// </summary>
	public Type SourcePageType { get; }
}
