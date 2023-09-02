using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

public partial class WebView : Control
{
	/// <summary>
	/// Gets or sets a value that indicates whether backward navigation is possible.
	/// </summary>
	public bool CanGoBack
	{
		get => (bool)GetValue(CanGoBackProperty);
		private set => SetValue(CanGoBackProperty, value);
	}

	/// <summary>
	/// Identifies the CanGoBack dependency property.
	/// </summary>
	public static DependencyProperty CanGoBackProperty { get; } =
		DependencyProperty.Register(nameof(CanGoBack), typeof(bool), typeof(WebView), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that indicates whether forward navigation is possible.
	/// </summary>
	public bool CanGoForward
	{
		get => (bool)GetValue(CanGoForwardProperty);
		private set => SetValue(CanGoForwardProperty, value);
	}

	/// <summary>
	/// Identifies the CanGoForward dependency property.
	/// </summary>
	public static DependencyProperty CanGoForwardProperty { get; } =
		DependencyProperty.Register(nameof(CanGoForward), typeof(bool), typeof(WebView), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets the URI of the current top level document.
	/// </summary>
	public Uri Source
	{
		get => (Uri)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Identifies the Source dependency property.
	/// </summary>
	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(WebView), new FrameworkPropertyMetadata(null,
			(s, e) =>
			{
				var webView = (WebView)s;
				if (!webView._sourceChangeFromCore)
				{
					webView.CoreWebView2.Navigate(((Uri)e.NewValue)?.ToString());
				}
			}));

	/// <summary>
	/// Gets the current web page's title.
	/// </summary>
	public string DocumentTitle
	{
		get => (string)GetValue(DocumentTitleProperty) ?? "";
		private set => SetValue(DocumentTitleProperty, value);
	}

	/// <summary>
	/// Identifies the DocumentTitle dependency property.
	/// </summary>
	public static DependencyProperty DocumentTitleProperty { get; } =
		DependencyProperty.Register(nameof(DocumentTitle), typeof(string), typeof(WebView), new FrameworkPropertyMetadata(string.Empty));

	public bool IsScrollEnabled
	{
		get => (bool)GetValue(IsScrollEnabledProperty);
		set => SetValue(IsScrollEnabledProperty, value);
	}

	public static DependencyProperty IsScrollEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsScrollEnabled),
			typeof(bool),
			typeof(WebView),
			new FrameworkPropertyMetadata(
				true,
				(s, e) => ((WebView)s)?.CoreWebView2.OnScrollEnabledChanged((bool)e.NewValue)));

#pragma warning disable 67
	/// <summary>
	/// Occurs before the WebView navigates to new content.
	/// </summary>
	public event TypedEventHandler<WebView, WebViewNavigationStartingEventArgs> NavigationStarting;

	/// <summary>
	/// Occurs when the WebView has finished loading the current content or if navigation has failed.
	/// </summary>
	public event TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> NavigationCompleted;

	/// <summary>
	/// Occurs when the WebView cannot complete the navigation attempt.
	/// </summary>
	public event WebViewNavigationFailedEventHandler NavigationFailed;

	/// <summary>
	/// Occurs when a user performs an action in a WebView that causes content to be opened in a new window.
	/// </summary>
	public event TypedEventHandler<WebView, WebViewNewWindowRequestedEventArgs> NewWindowRequested;

	/// <summary>
	/// Occurs when an attempt is made to navigate to a Uniform Resource Identifier (URI) using a scheme that WebView doesn't support.
	/// </summary>
	public event TypedEventHandler<WebView, WebViewUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified;
#pragma warning restore 67
}
