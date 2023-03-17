using Windows.Foundation;

namespace Microsoft.Web.WebView2.Core;
#pragma warning disable CS0067 // TODO:MZ: Undo this
public partial class CoreWebView2
{
	/// <summary>
	/// True if the WebView is able to navigate to a previous page in the navigation history.
	/// </summary>
	public bool CanGoBack { get; }

	/// <summary>
	/// True if the WebView is able to navigate to a next page in the navigation history.
	/// </summary>
	public bool CanGoForward { get; }

	/// <summary>
	/// Gets the title for the current top-level document.
	/// </summary>
	public string DocumentTitle { get; }

	/// <summary>
	/// Gets the URI of the current top level document.
	/// </summary>
	public string Source { get; }

	/// <summary>
	/// NavigationStarting is raised when the WebView main frame is requesting permission to navigate to a different URI.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2NavigationStartingEventArgs> NavigationStarting;

	/// <summary>
	/// DOMContentLoaded is raised when the initial HTML document has been parsed.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2DOMContentLoadedEventArgs> DOMContentLoaded;

	/// <summary>
	/// ContentLoading is raised before any content is loaded, including scripts added with CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync.
	/// ContentLoading is not raised if a same page navigation occurs (such as through fragment navigations or history.pushState navigations).
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2ContentLoadingEventArgs> ContentLoading;

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
	/// SourceChanged is raised when the CoreWebView2.Source property changes. SourceChanged is raised when
	/// navigating to a different site or fragment navigations.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2SourceChangedEventArgs> SourceChanged;
}
