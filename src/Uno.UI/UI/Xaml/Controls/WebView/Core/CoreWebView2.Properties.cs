using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Controls;

namespace Microsoft.Web.WebView2.Core;
#pragma warning disable CS0067 // TODO:MZ: Undo this
public partial class CoreWebView2
{
	private const string CoreWebView2TypeName = "Microsoft.Web.WebView2.Core.CoreWebView2";

	private string _source = "";
	private CoreWebView2Profile _profile;
	private CoreWebView2Environment _environment;

	/// <summary>
	/// Gets the process ID of the browser process that hosts the WebView.
	/// </summary>
	public uint BrowserProcessId
		=> RequireCapability<ISupportsWebViewEnvironmentInfo>(CoreWebView2TypeName, nameof(BrowserProcessId)).BrowserProcessId;

	/// <summary>
	/// Gets the CoreWebView2Environment this CoreWebView2 was created from.
	/// </summary>
	public CoreWebView2Environment Environment
	{
		get
		{
			if (_nativeWebView is not ISupportsWebViewEnvironmentInfo)
			{
				throw CapabilityUnavailable(CoreWebView2TypeName, nameof(Environment));
			}

			return _environment ??= new CoreWebView2Environment(this);
		}
	}

	/// <summary>
	/// Gets the profile this CoreWebView2 is running under.
	/// </summary>
	/// <remarks>
	/// A platform may implement profile metadata, browsing-data clearing, or both, so the facade is handed out
	/// when either is available and the individual members report what is missing.
	/// </remarks>
	public CoreWebView2Profile Profile
	{
		get
		{
			if (_nativeWebView is not (ISupportsWebViewProfile or ISupportsBrowsingDataClearing))
			{
				throw CapabilityUnavailable(CoreWebView2TypeName, nameof(Profile));
			}

			return _profile ??= new CoreWebView2Profile(this);
		}
	}

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
	/// HistoryChanged is raised for changes to joint session history, which consists of top-level and manual frame navigations.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, object> HistoryChanged;

	/// <summary>
	/// SourceChanged is raised when the CoreWebView2.Source property changes. SourceChanged is raised when
	/// navigating to a different site or fragment navigations.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2SourceChangedEventArgs> SourceChanged;

	/// <summary>
	/// Dispatches after web content sends a message to the app host.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2WebMessageReceivedEventArgs> WebMessageReceived;

	internal event TypedEventHandler<CoreWebView2, WebViewUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified;

	/// <summary>
	/// Occurs when an HTTP request is made in the WebView for a web resource.
	/// </summary>
	public event TypedEventHandler<CoreWebView2, CoreWebView2WebResourceRequestedEventArgs> WebResourceRequested;
}
