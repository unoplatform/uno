namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Defines properties that enable, disable, or modify WebView features.
/// </summary>
public partial class CoreWebView2Settings
{
	internal CoreWebView2Settings()
	{
	}

	/// <summary>
	/// Determines whether communication from the host
	/// to the top-level HTML document of the WebView is allowed.
	/// </summary>
	public bool IsWebMessageEnabled { get; set; } = true;
}
