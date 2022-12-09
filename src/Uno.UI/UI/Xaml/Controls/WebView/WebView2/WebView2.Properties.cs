using System;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class WebView2
{
	public bool CanGoBack
	{
		get => (bool)GetValue(CanGoBackProperty);
		set => SetValue(CanGoBackProperty, value);
	}

	public static DependencyProperty CanGoBackProperty { get; } =
		DependencyProperty.Regiater(nameof(CanGoBack), typeof(bool), typeof(WebView2), new FrameworkPropertyMetadata(false));

	public bool CanGoForward
	{
		get => (bool)GetValue(CanGoForwardProperty);
		set => SetValue(CanGoForwardProperty, value);
	}

	public static DependencyProperty CanGoForwardProperty { get; } =
		DependencyProperty.Regiater(nameof(CanGoForward), typeof(bool), typeof(WebView2), new FrameworkPropertyMetadata(false));

	public Uri Source
	{
		get => (Uri)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Regiater(nameof(Source), typeof(Uri), typeof(WebView2), new FrameworkPropertyMetadata(null,
			(s, e) => ((WebView)s)?.Navigate((Uri)e.NewValue)));

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
	/// Occurs when a new HTML document is loaded.
	/// </summary>
	public event TypedEventHandler<WebView2, CoreWebView2WebMessageReceivedEventArgs> WebMessageReceived;
}
