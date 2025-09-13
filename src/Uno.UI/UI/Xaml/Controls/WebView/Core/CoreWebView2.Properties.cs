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
	/// NavigationStarting is raised when the WebView main frame is requesting permission to navigate to a different URI.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2NavigationStartingEventArgs> NavigationStarting;

	/// <summary>
	/// NavigationCompleted is raised when the WebView has completely loaded (body.onload has been raised) or loading stopped with error.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2NavigationCompletedEventArgs> NavigationCompleted;

	/// <summary>
	/// NewWindowRequested is raised when content inside the WebView requests to open a new window, such as through window.open().
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2NewWindowRequestedEventArgs> NewWindowRequested;

	/// <summary>
	/// DocumentTitleChanged is raised when the CoreWebView2.DocumentTitle property changes and may be raised
	/// before or after the CoreWebView2.NavigationCompleted event.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, object> DocumentTitleChanged;

	/// <summary>
	/// HistoryChanged is raised for changes to joint session history, which consists of top-level and manual frame navigation.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, object> HistoryChanged;

	/// <summary>
	/// SourceChanged is raised when the CoreWebView2.Source property changes. SourceChanged is raised when
	/// navigating to a different site or fragment navigation.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2SourceChangedEventArgs> SourceChanged;

	/// <summary>
	/// Dispatches after web content sends a message to the app host.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2WebMessageReceivedEventArgs> WebMessageReceived;

	internal event TypedEventHandler<CoreWebView2, WebViewUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified;
}
