using System;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class WebView2
{
	/// <summary>
	/// Gets or sets a value that indicates whether backward navigation is possible.
	/// </summary>
	public bool CanGoBack
	{
		get => (bool)GetValue(CanGoBackProperty);
		set => SetValue(CanGoBackProperty, value);
	}

	/// <summary>
	/// Identifies the CanGoBack dependency property.
	/// </summary>
	public static DependencyProperty CanGoBackProperty { get; } =
		DependencyProperty.Register(nameof(CanGoBack), typeof(bool), typeof(WebView2), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that indicates whether forward navigation is possible.
	/// </summary>
	public bool CanGoForward
	{
		get => (bool)GetValue(CanGoForwardProperty);
		set => SetValue(CanGoForwardProperty, value);
	}

	/// <summary>
	/// Identifies the CanGoForward dependency property.
	/// </summary>
	public static DependencyProperty CanGoForwardProperty { get; } =
		DependencyProperty.Register(nameof(CanGoForward), typeof(bool), typeof(WebView2), new FrameworkPropertyMetadata(false));

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
		DependencyProperty.Register(nameof(Source), typeof(Uri), typeof(WebView2), new FrameworkPropertyMetadata(null,
			(s, e) =>
			{
				var webView = (WebView2)s;
				if (!webView._sourceChangeFromCore)
				{
					webView.CoreWebView2.Navigate(((Uri)e.NewValue)?.ToString());
				}
			}));

	public bool IsScrollEnabled
	{
		get => (bool)GetValue(IsScrollEnabledProperty);
		set => SetValue(IsScrollEnabledProperty, value);
	}

	public static DependencyProperty IsScrollEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsScrollEnabled),
			typeof(bool),
			typeof(WebView2),
			new FrameworkPropertyMetadata(
				true,
				(s, e) => ((WebView2)s)?.CoreWebView2.OnScrollEnabledChanged((bool)e.NewValue)));

#pragma warning disable 67
	/// <summary>
	/// Occurs when the core WebView2 process fails.
	/// </summary>
	public event TypedEventHandler<WebView2, CoreWebView2ProcessFailedEventArgs> CoreProcessFailed;

	/// <summary>
	/// Occurs when the WebView2 object is initialized.
	/// </summary>
	public event TypedEventHandler<WebView2, CoreWebView2InitializedEventArgs> CoreWebView2Initialized;

	/// <summary>
	/// Occurs when the WebView2 has completely loaded (body.onload has been raised) or loading stopped with error.
	/// </summary>
	public event TypedEventHandler<WebView2, CoreWebView2NavigationCompletedEventArgs> NavigationCompleted;

	/// <summary>
	/// Occurs when the main frame of the WebView2 navigates to a different URI.
	/// </summary>
	public event TypedEventHandler<WebView2, CoreWebView2NavigationStartingEventArgs> NavigationStarting;

	/// <summary>
	/// Dispatches after web content sends a message to the app host.
	/// </summary>
	public event TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs> WebMessageReceived;
}
