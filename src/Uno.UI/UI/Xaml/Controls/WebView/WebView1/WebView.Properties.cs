using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

public partial class WebView : Control
{
	public bool CanGoBack
	{
		get => (bool)GetValue(CanGoBackProperty);
		private set => SetValue(CanGoBackProperty, value);
	}

	public static DependencyProperty CanGoBackProperty { get; } =
		DependencyProperty.Regiater(nameof(CanGoBack), typeof(bool), typeof(WebView), new FrameworkPropertyMetadata(false));

	public bool CanGoForward
	{
		get => (bool)GetValue(CanGoForwardProperty);
		private set => SetValue(CanGoForwardProperty, value);
	}

	public static DependencyProperty CanGoForwardProperty { get; } =
		DependencyProperty.Regiater(nameof(CanGoForward), typeof(bool), typeof(WebView), new FrameworkPropertyMetadata(false));

	public Uri Source
	{
		get => (Uri)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Regiater(nameof(Source), typeof(Uri), typeof(WebView), new FrameworkPropertyMetadata(null,
			(s, e) => ((WebView)s)?.Navigate((Uri)e.NewValue)));

#if __ANDROID__ || __IOS__ || __MACOS__
	public string DocumentTitle
	{
		get => (string)GetValue(DocumentTitleProperty);
		internal set => SetValue(DocumentTitleProperty, value);
	}

	public static DependencyProperty DocumentTitleProperty { get; } =
		DependencyProperty.Register(nameof(DocumentTitle), typeof(string), typeof(WebView), new FrameworkPropertyMetadata(null));
#endif

	public bool IsScrollEnabled
	{
		get => (bool)GetValue(IsScrollEnabledProperty);
		set => SetValue(IsScrollEnabledProperty, value);
	}

	public static DependencyProperty IsScrollEnabledProperty { get; } =
		DependencyProperty.Regiater(nameof(IsScrollEnabled), typeof(bool), typeof(WebView), new FrameworkPropertyMetadata(true,
			(s, e) => ((WebView)s)?.OnScrollEnabledChangedPartial((bool)e.NewValue)));

	partial void OnScrollEnabledChangedPartial(bool scrollingEnabled);

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
