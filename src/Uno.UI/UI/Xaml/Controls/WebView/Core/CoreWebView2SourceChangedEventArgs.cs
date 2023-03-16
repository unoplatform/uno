namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the CoreWebView2.SourceChanged event. 
/// </summary>
public partial class CoreWebView2SourceChangedEventArgs
{
	/// <summary>
	/// True if the page being navigated to is a new document.
	/// </summary>
	public bool IsNewDocument { get; }
}
