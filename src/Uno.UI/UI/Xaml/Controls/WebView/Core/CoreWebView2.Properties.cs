using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;
#pragma warning disable CS0067 // TODO:MZ: Undo this
public partial class CoreWebView2
{
	private string _source = "";

	/// <summary>
	/// True if the WebView is able to navigate to a previous page in the navigation history.
	/// </summary>
	public bool CanGoBack { get; private set; }

	/// <summary>
	/// True if the WebView is able to navigate to a next page in the navigation history.
	/// </summary>
	public bool CanGoForward { get; private set; }

	/// <summary>
	/// Gets the title for the current top-level document.
	/// </summary>
	public string DocumentTitle => _nativeWebView?.DocumentTitle ?? "";

	/// <summary>
	/// Gets the URI of the current top level document.
	/// </summary>
	public string Source
	{
		get => _source;
		internal set
		{
			if (_source != value)
			{
				_source = value;
				SourceChanged?.Invoke(this, new());
			}
		}
	}

	/// <summary>
	/// <see cref="CoreWebView2.NavigationStarting"/> is raised when the WebView main frame is requesting permission to navigate to a different URI.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2NavigationStartingEventArgs> NavigationStarting;

	/// <summary>
	/// <see cref="CoreWebView2.NavigationCompleted"/> is raised when the WebView has completely loaded (body.onload has been raised) or loading stopped with error.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2NavigationCompletedEventArgs> NavigationCompleted;

	/// <summary>
	/// <see cref="CoreWebView2.NewWindowRequested"/> is raised when content inside the WebView requests to open a new window, such as through window.open().
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2NewWindowRequestedEventArgs> NewWindowRequested;

	/// <summary>
	/// <see cref="CoreWebView2.DocumentTitleChanged"/> event is raised when the <see cref="CoreWebView2.DocumentTitle"/> property changes and may be raised
	/// before or after the <see cref="CoreWebView2.NavigationCompleted"/> event.
	/// </summary>
	/// <remarks>
	/// The event data will always be <see langword="null"/>.<br/>
	/// The targeted Event Handling is meant to use the <see cref="CoreWebView2.DocumentTitle"/> property to get the current title directly from the sender instance.
	/// </remarks>
	public event TypedEventHandler<CoreWebView2, object> DocumentTitleChanged;

	/// <summary>
	/// <see cref="CoreWebView2.HistoryChanged"/> is raised for changes to joint session history, which consists of top-level and manual frame navigation.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, object> HistoryChanged;

	/// <summary>
	/// <see cref="CoreWebView2.SourceChanged"/> is raised when the <see cref="CoreWebView2.Source"/> property changes<br/>
	/// and when navigating to a different site or fragment navigation.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2SourceChangedEventArgs> SourceChanged;

	/// <summary>
	/// <see cref="CoreWebView2.WebMessageReceived"/> dispatches after web content sends a message to the app host.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2WebMessageReceivedEventArgs> WebMessageReceived;

	/// <summary>
	/// <see cref="CoreWebView2.UnsupportedUriSchemeIdentified"/> is raised when the WebView encounters a URI with an unsupported scheme.
	/// </summary>
	internal event TypedEventHandler<CoreWebView2, WebViewUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified;
}
