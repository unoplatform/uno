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

	/// <summary>
	/// Determines whether the user can open the DevTools window. The value is stored but not applied by the platforms.
	/// </summary>
	public bool AreDevToolsEnabled { get; set; } = true;

	/// <summary>
	/// Determines whether the default context menus are shown. The value is stored but not applied by the platforms.
	/// </summary>
	public bool AreDefaultContextMenusEnabled { get; set; } = true;

	/// <summary>
	/// Determines whether the status bar is displayed. The value is stored but not applied by the platforms.
	/// </summary>
	public bool IsStatusBarEnabled { get; set; } = true;
}
